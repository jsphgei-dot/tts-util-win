/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Text;

/// <summary>A list that ships with the program, off until it is asked for.</summary>
public sealed class AliasPack
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>One line saying what the list is for, shown beside its tick box.</summary>
    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<AliasRule> Rules { get; init; } = Array.Empty<AliasRule>();

    /// <summary>A copy of the rules, for adding them to a list the reader can edit.</summary>
    public AliasDictionary ToDictionary() =>
        new() { Name = Name, Rules = Rules.Select(rule => rule.Copy()).ToList() };
}

/// <summary>The lists that ship with the program. None of them is on until it is ticked.</summary>
public static class AliasPacks
{
    /// <summary>Symbols that are ordinary English words as well, which arrive turned off so a
    /// sentence starting with In or No is not read as an element.</summary>
    private static readonly HashSet<string> AlsoEnglishWords = new(StringComparer.Ordinal)
    {
        "I", "He", "Be", "As", "At", "In", "No", "Am", "Ho", "Np", "Pa", "Sc", "Ta",
    };

    private const string Elements =
        "H=hydrogen;He=helium;Li=lithium;Be=beryllium;B=boron;C=carbon;N=nitrogen;O=oxygen;" +
        "F=fluorine;Ne=neon;Na=sodium;Mg=magnesium;Al=aluminum;Si=silicon;P=phosphorus;S=sulfur;" +
        "Cl=chlorine;Ar=argon;K=potassium;Ca=calcium;Sc=scandium;Ti=titanium;V=vanadium;" +
        "Cr=chromium;Mn=manganese;Fe=iron;Co=cobalt;Ni=nickel;Cu=copper;Zn=zinc;Ga=gallium;" +
        "Ge=germanium;As=arsenic;Se=selenium;Br=bromine;Kr=krypton;Rb=rubidium;Sr=strontium;" +
        "Y=yttrium;Zr=zirconium;Nb=niobium;Mo=molybdenum;Tc=technetium;Ru=ruthenium;Rh=rhodium;" +
        "Pd=palladium;Ag=silver;Cd=cadmium;In=indium;Sn=tin;Sb=antimony;Te=tellurium;I=iodine;" +
        "Xe=xenon;Cs=cesium;Ba=barium;La=lanthanum;Ce=cerium;Pr=praseodymium;Nd=neodymium;" +
        "Pm=promethium;Sm=samarium;Eu=europium;Gd=gadolinium;Tb=terbium;Dy=dysprosium;" +
        "Ho=holmium;Er=erbium;Tm=thulium;Yb=ytterbium;Lu=lutetium;Hf=hafnium;Ta=tantalum;" +
        "W=tungsten;Re=rhenium;Os=osmium;Ir=iridium;Pt=platinum;Au=gold;Hg=mercury;Tl=thallium;" +
        "Pb=lead;Bi=bismuth;Po=polonium;At=astatine;Rn=radon;Fr=francium;Ra=radium;Ac=actinium;" +
        "Th=thorium;Pa=protactinium;U=uranium;Np=neptunium;Pu=plutonium;Am=americium;Cm=curium;" +
        "Bk=berkelium;Cf=californium;Es=einsteinium;Fm=fermium;Md=mendelevium;No=nobelium;" +
        "Lr=lawrencium;Rf=rutherfordium;Db=dubnium;Sg=seaborgium;Bh=bohrium;Hs=hassium;" +
        "Mt=meitnerium;Ds=darmstadtium;Rg=roentgenium;Cn=copernicium;Nh=nihonium;Fl=flerovium;" +
        "Mc=moscovium;Lv=livermorium;Ts=tennessine;Og=oganesson";

    private const string Compounds =
        "H2O=water;CO2=carbon dioxide;CO=carbon monoxide;O2=oxygen gas;N2=nitrogen gas;" +
        "H2=hydrogen gas;NaCl=sodium chloride;HCl=hydrochloric acid;H2SO4=sulfuric acid;" +
        "HNO3=nitric acid;NaOH=sodium hydroxide;KOH=potassium hydroxide;NH3=ammonia;CH4=methane;" +
        "C2H6=ethane;C2H5OH=ethanol;CH3OH=methanol;CaCO3=calcium carbonate;CaO=calcium oxide;" +
        "C6H12O6=glucose;H2O2=hydrogen peroxide;SO2=sulfur dioxide;NO2=nitrogen dioxide;" +
        "Fe2O3=iron three oxide;SiO2=silicon dioxide;ATP=A T P;DNA=D N A;RNA=R N A";

    private const string MathSymbols =
        "≈=approximately;≠=is not equal to;≤=is less than or equal to;≥=is greater than or equal to;" +
        "±=plus or minus;×=times;÷=divided by;√=the square root of;π=pi;∑=the sum of;∏=the product of;" +
        "∫=the integral of;∞=infinity;°=degrees;µ=micro;Δ=delta;θ=theta;λ=lambda;σ=sigma;Ω=ohms;" +
        "²=squared;³=cubed;½=a half;¼=a quarter;¾=three quarters";

    private const string Units =
        "km=kilometers;cm=centimeters;mm=millimeters;kg=kilograms;mg=milligrams;µg=micrograms;" +
        "ml=milliliters;kJ=kilojoules;kcal=kilocalories;kPa=kilopascals;kHz=kilohertz;MHz=megahertz;" +
        "GHz=gigahertz;Hz=hertz;kW=kilowatts;mph=miles per hour;kph=kilometers per hour;" +
        "rpm=revolutions per minute;psi=pounds per square inch;KB=kilobytes;MB=megabytes;" +
        "GB=gigabytes;TB=terabytes;Mbps=megabits per second;°C=degrees Celsius;°F=degrees Fahrenheit";

    private const string Shorthand =
        "e.g.=for example;i.e.=that is;etc.=et cetera;vs.=versus;approx.=approximately;" +
        "cf.=compare;et al.=and others;a.m.=A M;p.m.=P M;Dr.=Doctor;Mr.=Mister;Mrs.=Missus;" +
        "Ms.=Miz;Prof.=Professor;St.=Saint;No.=number;Fig.=figure;Ch.=chapter;p.=page;pp.=pages";

    /// <summary>Every list that ships, in the order they are shown.</summary>
    public static IReadOnlyList<AliasPack> All { get; } = new[]
    {
        new AliasPack
        {
            Id = "chemistry",
            Name = "Chemistry",
            Description = "Element symbols and common formulas, so K is read as potassium and "
                + "H2O as water. Symbols that are also English words arrive turned off.",
            Rules = Parse(Compounds, matchCase: true).Concat(Parse(Elements, matchCase: true)).ToList(),
        },
        new AliasPack
        {
            Id = "math",
            Name = "Math symbols",
            Description = "Signs a voice otherwise skips, such as the square root sign, pi and "
                + "the less than or equal to sign.",
            Rules = Parse(MathSymbols, matchCase: false, wholeWord: false),
        },
        new AliasPack
        {
            Id = "units",
            Name = "Units and measures",
            Description = "Short forms of the everyday units, from kilometers to megabytes.",
            Rules = Parse(Units, matchCase: true),
        },
        new AliasPack
        {
            Id = "shorthand",
            Name = "Everyday shorthand",
            Description = "The abbreviations that run through ordinary prose, such as e.g., i.e. "
                + "and etc.",
            Rules = Parse(Shorthand, matchCase: false),
        },
    };

    /// <summary>The list with this id, or null when it is one this version does not ship.</summary>
    public static AliasPack? Find(string id) =>
        All.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The rules from every list that is ticked, in the order the lists are shown.</summary>
    public static List<AliasRule> RulesFor(IEnumerable<string> ids)
    {
        var wanted = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        var rules = new List<AliasRule>();

        foreach (var pack in All)
        {
            if (!wanted.Contains(pack.Id)) continue;
            rules.AddRange(pack.Rules.Select(rule => rule.Copy()));
        }

        return rules;
    }

    private static List<AliasRule> Parse(string table, bool matchCase, bool wholeWord = true)
    {
        var rules = new List<AliasRule>();

        foreach (var entry in table.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var at = entry.IndexOf('=');
            if (at <= 0) continue;

            var match = entry[..at];

            rules.Add(new AliasRule
            {
                Match = match,
                SayAs = entry[(at + 1)..],
                WholeWord = wholeWord,
                MatchCase = matchCase,
                Enabled = !AlsoEnglishWords.Contains(match),
            });
        }

        return rules;
    }
}
