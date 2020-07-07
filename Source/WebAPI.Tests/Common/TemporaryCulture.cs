using System;
using System.Collections.Generic;
using System.Globalization;

namespace WebAPI.Tests.Common
{
    public class TemporaryCulture : IDisposable
    {
        private Stack<CultureInfo> savedCultures = new Stack<CultureInfo>();
        private bool disposed = false;

        public TemporaryCulture()
        {
        }

        public TemporaryCulture(string name) : this(new CultureInfo(name))
        { }

        public TemporaryCulture(CultureInfo culture)
        {
            Set(culture);
        }

        public void Set(string name) => Set(new CultureInfo(name));

        public void Set(CultureInfo culture)
        {
            savedCultures.Push(CultureInfo.CurrentCulture);
            CultureInfo.CurrentCulture = culture;
        }

        public void Reset()
        {
            if (savedCultures.Count > 0)
                CultureInfo.CurrentCulture = savedCultures.Pop();
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                Reset();
            }

            disposed = true;
        }
    }
}