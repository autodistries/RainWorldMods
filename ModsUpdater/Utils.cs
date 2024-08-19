

using System;
using System.Collections.Generic;
using IL.Menu;
using IL.MoreSlugcats;

public static class Utils {
    // Only supports . as a separator !
    public static bool IsVersionGreater(string a, string b) {
        if (!CheckVersionValidity(a) || !CheckVersionValidity(b)){
            
            return false;}
        
        List<Int32> versionA = VersionToList(a);
        List<Int32> versionB = VersionToList(b);

        for (int i = 0; i<Math.Max(versionA.Count, versionB.Count); i++) {
            if (versionA.Count > i && versionB.Count > i) {
                if (versionB[i]>versionA[i]) return true;
                else if (versionA[i] > versionB[i]) return false;
            } else if (versionB.Count>versionA.Count && versionB[i] != 0) return true;
        }
        return false;
    }

    public static List<int> VersionToList(string a)
    {
        List<int> res = new();
        string[] splittedA = a.Split('.');
        foreach(string s in splittedA) {
            if (Int32.TryParse(s, out int localVersion)) {
                res.Add(localVersion);
            } else {
                //separate string from rest
                // in no case should the version letter(s) be before an int!!
                string fp = "";
                string dp = "";
                for (int i=0; i<s.Length;i++) {
                    char c= s[i];
                    if (c >= 48 /*0*/ && c <= 57 /*9*/) {
                        fp += c.ToString();
                    } else if (c>=97 /*a*/ && c<=122 /*z*/) {
                        if (i!=s.Length-1) throw new FormatException("version string was not in correct format: found number after letter");                        
                        dp =(c-96).ToString();
                    } else throw new FormatException("version string was not in correct format: found foreign character");
                }
                if (fp.Length != 0) res.Add(Int32.Parse(fp));
                if (dp.Length!=0) res.Add(Int32.Parse(dp));
            }
        }
        return res;
    }

    /// <summary>
    /// checks version validity. Numbers only; one letter allowed at the end. Separator is .
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool CheckVersionValidity(string s) {
        for (int i=0; i<s.Length;i++) {
            char c = s[i];
            if (c >= 48 /*0*/ && c <= 57 /*9*/ || c>=97 /*a*/ && c<=122 /*z*/ || c=='.') {
                if (c>=97 /*a*/ && c<=122 /*z*/) {
                    if (i != s.Length-1) return false;
                }
            } else return false;
        }
        return true;
    }
}