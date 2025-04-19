using System.Reflection;
using LogicApp.Models;
using LogicApp.Tasks;
using Microsoft.Extensions.Logging;

namespace LogicApp.Services
{
    public class ExecutionStepLookup : IExecutionStepLookup
    {
        private ILogger<ExecutionStepLookup> _logger { get; set; } = null!;
        private IServiceProvider _serviceProvider { get; set; }
        private Dictionary<string, (Type, ExecutionStepMeta)> _executionStepTypesLookup { get; set; }

        public ExecutionStepLookup(IServiceProvider serviceProvider, ILogger<ExecutionStepLookup> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _executionStepTypesLookup = LoadExecutionStepTypesLookup();
        }

        private Dictionary<string, (Type, ExecutionStepMeta)> LoadExecutionStepTypesLookup()
        {
            try
            {
                var relevantTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.CustomAttributes.Count(ca => ca.AttributeType == typeof(ExecutionStepAttribute)) == 1);

                var typesWithAttrs = relevantTypes
                    .Where(t => t.IsAssignableFrom(typeof(ExecutionStep)))
                    .Select(t => (type: t, attr: (ExecutionStepMeta)t.GetCustomAttributesData()
                        .Single(a => a.AttributeType == typeof(ExecutionStepAttribute)).NamedArguments
                            .Single(a => a.MemberName == "Meta")
                            .TypedValue.Value!));
                              
                var lookup = typesWithAttrs.ToDictionary(typesWithAttrs => typesWithAttrs.Item2.LookupName, typesWithAttrs => (typesWithAttrs.Item1, typesWithAttrs.Item2));
                return lookup;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot reflectively load execution steps!");
                throw;
            }

        }

        public (Type, ExecutionStepMeta) LoadType(string lookupName)
        {
            if (_executionStepTypesLookup.TryGetValue(lookupName, out var type))
                return type;
            else
                throw new StepNotInitializedException($"{lookupName} was not found");
        }

        public void ValidateScopeForType(Scope scope, string lookupName)
        {
            var (_, meta) = LoadType(lookupName);

            if (scope.GlobalScope && !meta.AllowGlobalScope)
                throw new ScopeMismatchException($"{lookupName} cannot be executed in global scope");

            if (scope.TenantScope && !meta.AllowTenantScope)
                throw new ScopeMismatchException($"{lookupName} cannot be executed in tenant scope");
        }

        public ExecutionStep Load(string lookupName)
        {
            var (type, _) = LoadType(lookupName);

            var instance = _serviceProvider.GetService(type);
            if (instance == null)
                throw new StepNotInitializedException($"{lookupName} cannot be executed");

            return (instance as ExecutionStep)!;
        }

    }

    public class ScopeMismatchException(string message) : Exception(message) { }

    public class StepNotFoundException(string message) : Exception(message) { }

    public class StepNotInitializedException(string message) : Exception(message) { }
}
