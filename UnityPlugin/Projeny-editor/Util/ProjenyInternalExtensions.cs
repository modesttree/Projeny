
namespace Projeny.Internal
{
    public static class ProjenyInternalExtensions
    {
        // We'd prefer to use the name Format here but that conflicts with
        // the existing string.Format method
        public static string Fmt(this string s, params object[] args)
        {
            return string.Format(s, args);
        }
    }
}
