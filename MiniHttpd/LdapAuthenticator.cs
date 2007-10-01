using System;
using System.Collections.Specialized;
using System.DirectoryServices;

namespace MiniHttpd
{
    /// <summary>
    /// An authenticator that authenticates against an Active Directory server.
    /// </summary>
    [Serializable]
    public class LdapAuthenticator : IAuthenticator
    {
        private readonly string basePath;

        [NonSerialized] private readonly NameValueCollection _cache = new NameValueCollection();
        [NonSerialized] private DateTime _lastTimeout = DateTime.Now;

        [NonSerialized] private TimeSpan _maxCacheAge = new TimeSpan(0, 1, 0, 0, 0);

        /// <summary>
        /// Creates a new <see cref="LdapAuthenticator" /> based on the given base path.
        /// </summary>
        /// <param name="basePath">Base path to search users for in Active Directory.</param>
        public LdapAuthenticator(string basePath)
        {
            this.basePath = basePath;
        }

        /// <summary>
        /// Gets the base path to search users for in Active Directory.
        /// </summary>
        public string BasePath
        {
            get { return basePath; }
        }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan MaxCacheAge
        {
            get { return _maxCacheAge; }
            set { _maxCacheAge = value; }
        }

        #region IAuthenticator Members

        /// <summary>
        /// Authenticates against an LDAP server.
        /// </summary>
        /// <param name="username">The username to authenticate.</param>
        /// <param name="password">The password of the given user.</param>
        /// <returns>True if the user is successfully authenticated, otherwise false.</returns>
        public bool Authenticate(string username, string password)
        {
            if (_lastTimeout.Add(_maxCacheAge) < DateTime.Now)
                ResetCache();

            if (string.Compare(_cache[username], password, true) == 0)
                return true;

            try
            {
                new DirectoryEntry(basePath, username, password);
                _cache.Add(username, password);
            }
            catch
            {
                return false;
            }
            return true;
        }

        #endregion

        /// <summary>
        /// Resets the login cache.
        /// </summary>
        public void ResetCache()
        {
            _cache.Clear();
            _lastTimeout = DateTime.Now;
        }
    }
}