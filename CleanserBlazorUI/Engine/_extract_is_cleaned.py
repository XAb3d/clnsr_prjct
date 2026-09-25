"""One-off extractor: copy ISCleaned IND/BUS bodies from Home.razor into IsCleanedEvaluator.cs."""
from pathlib import Path

home = Path(__file__).resolve().parents[1] / "Components" / "Pages" / "Home.razor"
out = Path(__file__).resolve().parent / "IsCleanedEvaluator.cs"
lines = home.read_text(encoding="utf-8").splitlines()

helpers = "\n".join(lines[2530:2553])
add_err = "\n".join(lines[3086:3098])
bus_body = "\n".join(lines[6960:7591])
ind_body = "\n".join(lines[7594:8385])
utils = "\n".join(lines[8968:9162])


def bump(src: str, extra: str = "    ") -> str:
    return "\n".join(extra + ln if ln.strip() else ln for ln in src.splitlines())


header = r'''using CleanserBlazorUI.Entities;
using CleanserBlazorUI.Helpers;

namespace CleanserBlazorUI.Engine;

/// <summary>
/// Row-level CLEAN vs UNL decision copied from Home.razor ISCleaned for IND and BUS.
/// Cross-record flags are left empty when called from the CKMS API.
/// </summary>
public class IsCleanedEvaluator
{
    readonly StringHelper stringHelper = new();
    readonly List<string> settingDataSettings;
    readonly Dictionary<string, string> shortForms;

    public IsCleanedEvaluator(
        List<string>? businessKeywords = null,
        Dictionary<string, string>? shortFormMap = null)
    {
        settingDataSettings = businessKeywords ?? new List<string>();
        shortForms = shortFormMap ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public bool Evaluate(BusinessContext bc)
    {
        if (bc == null) return false;
'''

footer = "\n}\n"

text = "\n".join(
    [
        header,
        bump(bus_body, "        "),
        "    }\n",
        "    public bool Evaluate(IndividualContext ic)\n    {\n        if (ic == null) return false;\n",
        bump(ind_body, "        "),
        "    }\n",
        bump(helpers, "    "),
        "\n",
        bump(add_err, "    "),
        "\n",
        bump(utils, "    "),
        footer,
    ]
)
out.write_text(text, encoding="utf-8")
print(f"Wrote {out} ({out.stat().st_size} bytes)")
