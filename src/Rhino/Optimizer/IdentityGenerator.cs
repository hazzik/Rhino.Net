namespace Rhino.Optimizer
{
	internal class IdentityGenerator
	{
		private int id;
		
		public int GetNextId()
		{
			return ++id;
		} 
	}
}