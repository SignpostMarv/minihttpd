using System.Collections;
using System.Globalization;

namespace MiniHttpd
{
    internal class CaseInsensitiveEqualityComparer : IEqualityComparer
    {
        public CaseInsensitiveComparer myComparer;

        public CaseInsensitiveEqualityComparer()
        {
            myComparer = CaseInsensitiveComparer.DefaultInvariant;
        }

        public CaseInsensitiveEqualityComparer(CultureInfo myCulture)
        {
            myComparer = new CaseInsensitiveComparer(myCulture);
        }

        #region IEqualityComparer Members

        public new bool Equals(object x, object y)
        {
            if (myComparer.Compare(x, y) == 0)
                return true;
            else
                return false;
        }

        public int GetHashCode(object obj)
        {
            return obj.ToString().ToLower().GetHashCode();
        }

        #endregion
    }
}