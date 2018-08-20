namespace Fluent.Net.Test
{
    public class FtlTestBase
    {
        protected static string Ftl(string input) => Util.Ftl(input);
        
        protected Ast.Span Span(int start, int end)
        {
            return new Ast.Span(
                new Position(start, 1, 1), new Position(end, 1, 1 + end));
        }
    }
}
