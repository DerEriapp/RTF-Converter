using System.Text.RegularExpressions;
using System.Text;

void main()
{
    // Sample RTF text (potentially incorrect format)
    string rtfText = "{\\rtf1\\ansi\\Hello, World!}";

    // Attempt to convert RTF to plain text
    try
    {
        string plainText = rtf_to_txt(rtfText);
    }
    catch (Exception e)
    {
        //An error occurred during the conversion
    }
}


string rtf_to_txt(string rtf)
{
    var pattern = new Regex(
        @"\\([a-z]{1,32})(-?\d{1,10})?[ ]?|\\'([0-9a-f]{2})|\\([^a-z])|([{}])|[\r\n]+|(.)",
        RegexOptions.IgnoreCase);

    var destinations = new HashSet<string>
        {
            "aftncn","aftnsep","aftnsepc","annotation","atnauthor","atndate","atnicn","atnid",
            "atnparent","atnref","atntime","atrfend","atrfstart","author","background",
            "bkmkend","bkmkstart","blipuid","buptim","category","colorschememapping",
            "colortbl","comment","company","creatim","datafield","datastore","defchp","defpap",
            "do","doccomm","docvar","dptxbxtext","ebcend","ebcstart","factoidname","falt",
            "fchars","ffdeftext","ffentrymcr","ffexitmcr","ffformat","ffhelptext","ffl",
            "ffname","ffstattext","field","file","filetbl","fldinst","fldrslt","fldtype",
            "fname","fontemb","fontfile","fonttbl","footer","footerf","footerl",
            "footerr","footnote","formfield","ftncn","ftnsep","ftnsepc","g","generator",
            "gridtbl","header","headerf","headerl","headerr","hl","hlfr","hlinkbase",
            "hlloc","hlsrc","hsv","htmltag","info","keycode","keywords","latentstyles",
            "lchars","levelnumbers","leveltext","lfolevel","linkval","list","listlevel",
            "listname","listoverride","listoverridetable","listpicture","liststylename",
            "listtable","listtext","lsdlockedexcept","macc","maccPr","mailmerge","maln",
            "malnScr","manager","margPr","mbar","mbarPr","mbaseJc","mbegChr","mborderBox",
            "mborderBoxPr","mbox","mboxPr","mchr","mcount","mctrlPr","md","mdeg","mdegHide",
            "mden","mdiff","mdPr","me","mendChr","meqArr","meqArrPr","mf","mfName","mfPr",
            "mfunc","mfuncPr","mgroupChr","mgroupChrPr","mgrow","mhideBot","mhideLeft",
            "mhideRight","mhideTop","mhtmltag","mlim","mlimloc","mlimlow","mlimlowPr",
            "mlimupp","mlimuppPr","mm","mmaddfieldname","mmath","mmathPict","mmathPr",
            "mmaxdist","mmc","mmcJc","mmconnectstr","mmconnectstrdata","mmcPr","mmcs",
            "mmdatasource","mmheadersource","mmmailsubject","mmodso","mmodsofilter",
            "mmodsofldmpdata","mmodsomappedname","mmodsoname","mmodsorecipdata","mmodsosort",
            "mmodsosrc","mmodsotable","mmodsoudl","mmodsoudldata","mmodsouniquetag",
            "mmPr","mmquery","mmr","mnary","mnaryPr","mnoBreak","mnum","mobjDist","moMath",
            "moMathPara","moMathParaPr","mopEmu","mphant","mphantPr","mplcHide","mpos",
            "mr","mrad","mradPr","mrPr","msepChr","mshow","mshp","msPre","msPrePr","msSub",
            "msSubPr","msSubSup","msSubSupPr","msSup","msSupPr","mstrikeBLTR","mstrikeH",
            "mstrikeTLBR","mstrikeV","msub","msubHide","msup","msupHide","mtransp","mtype",
            "mvertJc","mvfmf","mvfml","mvtof","mvtol","mzeroAsc","mzeroDesc","mzeroWid",
            "nesttableprops","nextfile","nonesttables","objalias","objclass","objdata",
            "object","objname","objsect","objtime","oldcprops","oldpprops","oldsprops",
            "oldtprops","oleclsid","operator","panose","password","passwordhash","pgp",
            "pgptbl","picprop","pict","pn","pnseclvl","pntext","pntxta","pntxtb","printim",
            "private","propname","protend","protstart","protusertbl","pxe","result",
            "revtbl","revtim","rsidtbl","rxe","shp","shpgrp","shpinst",
            "shppict","shprslt","shptxt","sn","sp","staticval","stylesheet","subject","sv",
            "svb","tc","template","themedata","title","txe","ud","upr","userprops",
            "wgrffmtfilter","windowcaption","writereservation","writereservhash","xe","xform",
            "xmlattrname","xmlattrvalue","xmlclose","xmlname","xmlnstbl","xmlopen",
        };

    var specialchars = new Dictionary<string, string>
        {
            { "par", "\n" },
            { "sect", "\n\n" },
            { "page", "\n\n" },
            { "line", "\n" },
            { "tab", "\t" },
            { "emdash", "\u2014" },
            { "endash", "\u2013" },
            { "emspace", "\u2003" },
            { "enspace", "\u2002" },
            { "qmspace", "\u2005" },
            { "bullet", "\u2022" },
            { "lquote", "\u2018" },
            { "rquote", "\u2019" },
            { "ldblquote", "\u201C" },
            { "rdblquote", "\u201D" },
        };

    var stack = new Stack<(int, bool)>();
    bool ignorable = false;
    int ucskip = 1;
    int curskip = 0;
    var outText = new StringBuilder();

    foreach (Match match in pattern.Matches(rtf))
    {
        var word = match.Groups[1].Value;
        var arg = match.Groups[2].Value;
        var hex = match.Groups[3].Value;
        var chr = match.Groups[4].Value;
        var brace = match.Groups[5].Value;
        var tchar = match.Groups[6].Value;

        if (!string.IsNullOrEmpty(brace))
        {
            curskip = 0;
            if (brace == "{")
            {
                stack.Push((ucskip, ignorable));
            }
            else if (brace == "}")
            {
                (ucskip, ignorable) = stack.Pop();
            }
        }
        else if (!string.IsNullOrEmpty(chr))
        {
            curskip = 0;
            if (chr == "~" && !ignorable)
            {
                outText.Append('\u00A0');
            }
            else if (chr == "{" || chr == "}" || chr == "\\")
            {
                if (!ignorable)
                {
                    outText.Append(chr);
                }
            }
            else if (chr == "*")
            {
                ignorable = true;
            }
        }
        else if (!string.IsNullOrEmpty(word))
        {
            curskip = 0;
            if (destinations.Contains(word))
            {
                ignorable = true;
            }
            else if (ignorable)
            {
                // Skip control word
            }
            else if (specialchars.TryGetValue(word, out var special))
            {
                outText.Append(special);
            }
            else if (word == "uc")
            {
                ucskip = int.Parse(arg);
            }
            else if (word == "u")
            {
                int c = int.Parse(arg);
                if (c < 0) c += 0x10000;
                outText.Append((char)c);
                curskip = ucskip;
            }
        }
        else if (!string.IsNullOrEmpty(hex))
        {
            if (curskip > 0)
            {
                curskip--;
            }
            else if (!ignorable)
            {
                int c = Convert.ToInt32(hex, 16);
                outText.Append((char)c);
            }
        }
        else if (!string.IsNullOrEmpty(tchar))
        {
            if (curskip > 0)
            {
                curskip--;
            }
            else if (!ignorable)
            {
                outText.Append(tchar);
            }
        }
    }

    return outText.ToString().Trim();
}
