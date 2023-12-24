﻿namespace Fiero.Core
{
    public class GameScripts<TScripts>(IScriptHost<TScripts> host)
        where TScripts : struct, Enum
    {
        protected readonly Dictionary<string, Script> Scripts = new();
        public readonly IScriptHost<TScripts> Host = host;

        private static string CacheKey(TScripts a, string b) => $"{a}{b}";

        public bool TryLoad(TScripts key, out Script script, string cacheKey = null)
        {
            if (Host.TryLoad(key, out script))
            {
                Scripts[CacheKey(key, cacheKey)] = script;
                return true;
            }
            return false;
        }
        public bool TryGet(TScripts key, out Script script, string cacheKey = null) => Scripts.TryGetValue(CacheKey(key, cacheKey), out script);
    }
}