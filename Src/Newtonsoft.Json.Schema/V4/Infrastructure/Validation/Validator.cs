using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.V4.Infrastructure.Validation;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Schema.V4.Infrastructure.Validation
{
    internal abstract class Validator
    {
        private readonly List<Scope> _scopes;
        private readonly object _publicValidator;
        private readonly ValidatorContext _context;
        
        public JTokenWriter TokenWriter;
        public JSchema4 Schema;
        public event SchemaValidationEventHandler ValidationEventHandler;

        public List<Scope> Scopes
        {
            get { return _scopes; }
        }

        public abstract ISchemaError CreateError(string message, JSchema4 schema, IList<ISchemaError> childErrors);


        protected Validator(object publicValidator)
        {
            _publicValidator = publicValidator;
            _scopes = new List<Scope>();
            _context = new ValidatorContext(this);
        }

        public void RaiseError(string message, JSchema4 schema, IList<ISchemaError> childErrors)
        {
            JSchemaException ex = (JSchemaException)CreateError(message, schema, childErrors);

            SchemaValidationEventHandler handler = ValidationEventHandler;
            if (handler != null)
                handler(_publicValidator, new SchemaValidationEventArgs(ex));
            else
                throw ex;
        }

        protected ISchemaError CreateError(string message, JSchema4 schema, IList<ISchemaError> childErrors, IJsonLineInfo lineInfo, string path)
        {
            string exceptionMessage = (lineInfo != null && lineInfo.HasLineInfo())
                ? message + " Line {0}, position {1}.".FormatWith(CultureInfo.InvariantCulture, lineInfo.LineNumber, lineInfo.LinePosition)
                : message;

            JSchemaException exception = new JSchemaException(exceptionMessage, null);
            exception.Path = path;
            if (lineInfo != null)
            {
                exception.LineNumber = lineInfo.LineNumber;
                exception.LinePosition = lineInfo.LinePosition;
            }
            exception.Schema = schema;
            exception.ChildErrors = childErrors ?? new List<ISchemaError>();

            return exception;
        }

        public void ValidateCurrentToken(JsonToken token, object value, int depth)
        {
            if (_scopes.Count == 0)
            {
                if (Schema == null)
                    throw new JsonException("No schema has been set for the validator.");

                SchemaScope.CreateTokenScope(token, Schema, _context, null, depth);
            }

            if (TokenWriter != null)
                TokenWriter.WriteToken(token, value);

            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                Scope scope = _scopes[i];

                scope.EvaluateToken(token, value, depth);
            }

            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                Scope scope = _scopes[i];

                if (scope.Complete)
                    _scopes.RemoveAt(i);
            }

            if (TokenWriter != null && TokenWriter.Top == 0)
                TokenWriter = null;
        }
    }
}