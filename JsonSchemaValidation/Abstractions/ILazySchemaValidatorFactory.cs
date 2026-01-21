// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface ILazySchemaValidatorFactory
    {
        public ISchemaValidatorFactory? Value { get; set; }
    }
}
