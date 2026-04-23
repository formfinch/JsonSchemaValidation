import Ajv2020 from 'ajv/dist/2020.js';
import addFormats from 'ajv-formats';
import * as Hyperjump from '@hyperjump/json-schema/draft-2020-12';
import { Validator as CfworkerValidator } from '@cfworker/json-schema';
import { createInterface } from 'readline';
import { pathToFileURL } from 'url';

let currentLibrary = null;
let schemaCounter = 0;

// Library-specific state
let ajvValidator = null;
let hyperjumpSchemaUri = null;
let cfworkerValidator = null;
let generatedValidator = null;
let preparedData = undefined;

function createAjv() {
    const instance = new Ajv2020({
        allErrors: false,
        strict: false
    });
    addFormats(instance);
    return instance;
}

const rl = createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: false
});

function respond(obj) {
    console.log(JSON.stringify(obj));
}

async function validatePrepared(data) {
    if (currentLibrary === 'ajv') {
        if (!ajvValidator) {
            throw new Error('No schema prepared');
        }
        return !!ajvValidator(data);
    }

    if (currentLibrary === 'hyperjump') {
        if (!hyperjumpSchemaUri) {
            throw new Error('No schema prepared');
        }
        const output = await Hyperjump.validate(hyperjumpSchemaUri, data);
        return !!output.valid;
    }

    if (currentLibrary === 'cfworker') {
        if (!cfworkerValidator) {
            throw new Error('No schema prepared');
        }
        const result = cfworkerValidator.validate(data);
        return !!result.valid;
    }

    if (currentLibrary === 'formfinch-js') {
        if (!generatedValidator) {
            throw new Error('No generated validator prepared');
        }
        return !!generatedValidator(data);
    }

    throw new Error('No library selected');
}

async function handleCommand(command) {
    try {
        const cmd = command.Cmd || command.cmd;
        const library = command.Library || command.library || 'ajv';

        switch (cmd) {
            case 'prepare':
                currentLibrary = library.toLowerCase();

                if (currentLibrary === 'ajv') {
                    const schemaText = command.Schema || command.schema;
                    const schema = JSON.parse(schemaText);
                    const ajv = createAjv();
                    ajvValidator = ajv.compile(schema);
                    respond({ Success: true });
                } else if (currentLibrary === 'hyperjump') {
                    const schemaText = command.Schema || command.schema;
                    const schema = JSON.parse(schemaText);
                    schemaCounter++;
                    hyperjumpSchemaUri = `https://benchmark.local/schema-${schemaCounter}`;

                    let schemaToRegister;
                    if (typeof schema === 'boolean') {
                        schemaToRegister = {
                            "$id": hyperjumpSchemaUri,
                            "$schema": "https://json-schema.org/draft/2020-12/schema"
                        };
                        if (schema === false) {
                            schemaToRegister.not = {};
                        }
                    } else {
                        schemaToRegister = schema;
                        schemaToRegister.$id = hyperjumpSchemaUri;
                        if (!schemaToRegister.$schema) {
                            schemaToRegister.$schema = "https://json-schema.org/draft/2020-12/schema";
                        }
                    }

                    try {
                        Hyperjump.unregisterSchema(hyperjumpSchemaUri);
                    } catch (e) { }

                    Hyperjump.registerSchema(schemaToRegister, hyperjumpSchemaUri);
                    respond({ Success: true });
                } else if (currentLibrary === 'cfworker') {
                    const schemaText = command.Schema || command.schema;
                    const schema = JSON.parse(schemaText);
                    cfworkerValidator = new CfworkerValidator(schema, '2020-12', false);
                    respond({ Success: true });
                } else if (currentLibrary === 'formfinch-js') {
                    const modulePath = command.ModulePath || command.modulePath;
                    if (!modulePath) {
                        respond({ Success: false, Error: 'modulePath is required for formfinch-js' });
                        return;
                    }

                    const moduleUrl = `${pathToFileURL(modulePath).href}?v=${++schemaCounter}`;
                    const module = await import(moduleUrl);
                    const validate = module.validate ?? module.default?.validate;
                    if (typeof validate !== 'function') {
                        respond({ Success: false, Error: 'Generated module does not export validate()' });
                        return;
                    }

                    generatedValidator = validate;
                    respond({ Success: true });
                } else {
                    respond({ Success: false, Error: `Unknown library: ${library}` });
                }
                break;

            case 'prepare-data':
                preparedData = JSON.parse(command.Data || command.data);
                respond({ Success: true });
                break;

            case 'validate':
                const data = JSON.parse(command.Data || command.data);
                respond({ Success: true, Valid: await validatePrepared(data) });
                break;

            case 'validate-prepared':
                if (preparedData === undefined) {
                    respond({ Success: false, Error: 'No prepared data' });
                    return;
                }
                respond({ Success: true, Valid: await validatePrepared(preparedData) });
                break;

            case 'benchmark':
                const benchData = JSON.parse(command.Data || command.data);
                const iterations = command.Iterations || command.iterations || 1000;
                const timings = [];

                for (let i = 0; i < iterations; i++) {
                    const start = process.hrtime.bigint();
                    await validatePrepared(benchData);
                    const end = process.hrtime.bigint();
                    timings.push(Number(end - start) / 1000);
                }

                respond({ Success: true, Timings: timings });
                break;

            case 'benchmark-prepared':
                if (preparedData === undefined) {
                    respond({ Success: false, Error: 'No prepared data' });
                    return;
                }

                const preparedIterations = command.Iterations || command.iterations || 1000;
                let validCount = 0;
                for (let i = 0; i < preparedIterations; i++) {
                    if (await validatePrepared(preparedData)) {
                        validCount++;
                    }
                }

                respond({ Success: true, ValidCount: validCount });
                break;

            case 'benchmark-full':
                // Measures full end-to-end time (schema compilation + validation) per iteration
                const fullSchemaText = command.Schema || command.schema;
                const fullBenchData = JSON.parse(command.Data || command.data);
                const fullIterations = command.Iterations || command.iterations || 1000;
                const fullTimings = [];
                const lib = (command.Library || command.library || currentLibrary || 'ajv').toLowerCase();

                if (lib === 'ajv') {
                    for (let i = 0; i < fullIterations; i++) {
                        const start = process.hrtime.bigint();
                        const ajv = createAjv();
                        const schema = JSON.parse(fullSchemaText);
                        const validate = ajv.compile(schema);
                        validate(fullBenchData);
                        const end = process.hrtime.bigint();
                        fullTimings.push(Number(end - start) / 1000);
                    }
                } else if (lib === 'hyperjump') {
                    for (let i = 0; i < fullIterations; i++) {
                        const start = process.hrtime.bigint();
                        schemaCounter++;
                        const uri = `https://benchmark.local/schema-full-${schemaCounter}`;
                        const schema = JSON.parse(fullSchemaText);

                        let schemaToRegister;
                        if (typeof schema === 'boolean') {
                            schemaToRegister = {
                                "$id": uri,
                                "$schema": "https://json-schema.org/draft/2020-12/schema"
                            };
                            if (schema === false) {
                                schemaToRegister.not = {};
                            }
                        } else {
                            schemaToRegister = schema;
                            schemaToRegister.$id = uri;
                            if (!schemaToRegister.$schema) {
                                schemaToRegister.$schema = "https://json-schema.org/draft/2020-12/schema";
                            }
                        }

                        Hyperjump.registerSchema(schemaToRegister, uri);
                        await Hyperjump.validate(uri, fullBenchData);
                        const end = process.hrtime.bigint();
                        fullTimings.push(Number(end - start) / 1000);

                        try { Hyperjump.unregisterSchema(uri); } catch (e) { }
                    }
                } else if (lib === 'cfworker') {
                    for (let i = 0; i < fullIterations; i++) {
                        const start = process.hrtime.bigint();
                        const schema = JSON.parse(fullSchemaText);
                        const validator = new CfworkerValidator(schema, '2020-12', false);
                        validator.validate(fullBenchData);
                        const end = process.hrtime.bigint();
                        fullTimings.push(Number(end - start) / 1000);
                    }
                } else {
                    respond({ Success: false, Error: `Unknown library for full benchmark: ${lib}` });
                    return;
                }

                respond({ Success: true, Timings: fullTimings });
                break;

            case 'exit':
                respond({ Success: true });
                process.exit(0);
                break;

            default:
                respond({ Success: false, Error: `Unknown command: ${cmd}` });
        }
    } catch (err) {
        respond({ Success: false, Error: err.message });
    }
}

rl.on('line', async (line) => {
    try {
        const command = JSON.parse(line);
        await handleCommand(command);
    } catch (err) {
        respond({ Success: false, Error: `Failed to parse command: ${err.message}` });
    }
});

console.log('ready');
