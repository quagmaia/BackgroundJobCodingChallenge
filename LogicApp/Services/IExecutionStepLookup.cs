using LogicApp.Models;
using LogicApp.Tasks;

namespace LogicApp.Services
{
    public interface IExecutionStepLookup
    {
        ExecutionStep Load(string lookupName);
        (Type, ExecutionStepMeta) LoadType(string lookupName);
        void ValidateScopeForType(Scope scope, string lookupName);
    }
}