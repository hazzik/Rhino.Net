namespace Rhino.Tools.Debugger
{
    internal class ScopeProviderImpl : ScopeProvider
    {
        private readonly Scriptable _scope;

        public ScopeProviderImpl(Scriptable scope)
        {
            _scope = scope;
        }

        public Scriptable GetScope()
        {
            return _scope;
        }
    }
}