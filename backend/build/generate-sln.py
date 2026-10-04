"""Writes backend/ProCargo.sln listing every project under src/ and tests/.
Run it after adding a project:  python3 build/generate-sln.py"""
import pathlib, uuid

root = pathlib.Path(__file__).resolve().parent.parent
CS = "9A19103F-16F7-4668-BE54-9A1E7A4F7556"
FOLDER = "2150E333-8FDC-42A3-9474-1A3956D46DE8"

def guid(name):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, "procargo/" + name)).upper()

lines = ["", "Microsoft Visual Studio Solution File, Format Version 12.00", "# Visual Studio Version 17",
         "VisualStudioVersion = 17.0.31903.59", "MinimumVisualStudioVersion = 10.0.40219.1"]
nested, configs = [], []
for folder in ["src", "tests"]:
    fid = guid("folder/" + folder)
    lines += [f'Project("{{{FOLDER}}}") = "{folder}", "{folder}", "{{{fid}}}"', "EndProject"]
    for proj in sorted((root / folder).glob("*/*.csproj")):
        pid = guid(proj.stem)
        rel = proj.relative_to(root).as_posix().replace("/", "\\")
        lines += [f'Project("{{{CS}}}") = "{proj.stem}", "{rel}", "{{{pid}}}"', "EndProject"]
        nested.append(f"\t\t{{{pid}}} = {{{fid}}}")
        for c in ["Debug|Any CPU", "Release|Any CPU"]:
            configs += [f"\t\t{{{pid}}}.{c}.ActiveCfg = {c}", f"\t\t{{{pid}}}.{c}.Build.0 = {c}"]
lines += ["Global",
          "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution",
          "\t\tDebug|Any CPU = Debug|Any CPU", "\t\tRelease|Any CPU = Release|Any CPU", "\tEndGlobalSection",
          "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution", *configs, "\tEndGlobalSection",
          "\tGlobalSection(SolutionProperties) = preSolution", "\t\tHideSolutionNode = FALSE", "\tEndGlobalSection",
          "\tGlobalSection(NestedProjects) = preSolution", *nested, "\tEndGlobalSection",
          "EndGlobal", ""]
(root / "ProCargo.sln").write_text("\r\n".join(lines), encoding="utf-8-sig")
print("wrote", root / "ProCargo.sln")
