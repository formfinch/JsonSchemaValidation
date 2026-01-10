import Ajv2020 from 'ajv/dist/2020.js';
import addFormats from 'ajv-formats';
import * as Hyperjump from '@hyperjump/json-schema/draft-2020-12';
import { Validator as CfworkerValidator } from '@cfworker/json-schema';
import { createInterface } from 'readline';

let currentLibrary = null;
let schemaCounter = 0;

// Library-specific state
let ajvValidator = null;
let hyperjumpSchemaUri = null;
let cfworkerValidator = null;

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

async function handleCommand(command) {
    try {
        const cmd = command.Cmd || command.cmd;
        const library = command.Library || command.library || 'ajv';

        switch (cmd) {
            case 'prepare':
                currentLibrary = library.toLowerCase();
                const schemaText = command.Schema || command.schema;
                const schema = JSON.parse(schemaText);

                if (currentLibrary === 'ajv') {
                    const ajv = createAjv();
                    ajvValidator = ajv.compile(schema);
                    respond({ Success: true });
                } else if (currentLibrary === 'hyperjump') {
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
                    cfworkerValidator = new CfworkerValidator(schema, '2020-12', false);
                    respond({ Success: true });
                } else {
                    respond({ Success: false, Error: `Unknown library: ${library}` });
                }
                break;

            case 'validate':
                const data = JSON.parse(command.Data || command.data);

                if (currentLibrary === 'ajv') {
                    if (!ajvValidator) {
                        respond({ Success: false, Error: 'No schema prepared' });
                        return;
                    }
                    const valid = ajvValidator(data);
                    respond({ Success: true, Valid: valid });
                } else if (currentLibrary === 'hyperjump') {
                    if (!hyperjumpSchemaUri) {
                        respond({ Success: false, Error: 'No schema prepared' });
                        return;
                    }
                    const output = await Hyperjump.validate(hyperjumpSchemaUri, data);
                    respond({ Success: true, Valid: output.valid });
                } else if (currentLibrary === 'cfworker') {
                    if (!cfworkerValidator) {
                        respond({ Success: false, Error: 'No schema prepared' });
                        return;
                    }
                    const result = cfworkerValidator.validate(data);
                    respond({ Success: true, Valid: result.valid });
                } else {
                    respond({ Success: false, Error: 'No library selected' });
                }
                break;

            case 'benchmark':
                const benchData = JSON.parse(command.Data || command.data);
                const iterations = command.Iterations || command.iterations || 1000;
                const timings = [];

                if (currentLibrary === 'ajv') {
                    if (!ajvValidator) {
                        respond({ Success: false, Error: 'No schema prepared' });
                        return;
                    }
                    for (let i = 0; i < iterations; i++) {
                        const start = process.hrtime.bigint();
                        ajvValidator(benchData);
                        const end = process.hrtime.bigint();
                        timings.push(Number(end - start) / 1000);
                    }
                } else if (currentLibrary === 'hyperjump') {
                    if (!hyperjumpSchemaUri) {
                        respond({ Success: false, Error: 'No schema prepared' });
                        return;
                    }
                    for (let i = 0; i < iterations; i++) {
                        const start = process.hrtime.bigint();
                        await Hyperjump.validate(hyperjumpSchemaUri, benchData);
                        const end = process.hrtime.bigint();
                        timings.push(Number(end - start) / 1000);
                    }
                } else if (currentLibrary === 'cfworker') {
                    if (!cfworkerValidator) {
                        respond({ Success: false, Error: 'No schema prepared' });
                        return;
                    }
                    for (let i = 0; i < iterations; i++) {
                        const start = process.hrtime.bigint();
                        cfworkerValidator.validate(benchData);
                        const end = process.hrtime.bigint();
                        timings.push(Number(end - start) / 1000);
                    }
                } else {
                    respond({ Success: false, Error: 'No library selected' });
                    return;
                }

                respond({ Success: true, Timings: timings });
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
