namespace Rhino.Tools.Debugger
{
	internal sealed class ListenerImpl : ContextFactory.Listener
	{
		internal ListenerImpl(Dim dim)
		{
			this.dim = dim;
		}

		/// <summary>The debugger.</summary>
		private readonly Dim dim;

		/// <summary>Called when a Context is created.</summary>
		public void ContextCreated(Context cx)
		{
			cx.SetDebugger(new DebuggerImpl(dim), new ContextData());
			cx.SetGeneratingDebug(true);
			cx.SetOptimizationLevel(-1);
		}

		/// <summary>Called when a Context is destroyed.</summary>
		public void ContextReleased(Context cx)
		{
		}
	}
}