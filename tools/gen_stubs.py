#!/usr/bin/env python3
"""Iteratively grow compile-only game stubs from C# compiler errors."""
from __future__ import annotations

import json
import os
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path("/workspace")
STUBS = ROOT / "tools" / "stubs"
GENERATED = STUBS / "Assembly-CSharp" / "Generated.cs"
STATE_PATH = STUBS / "Assembly-CSharp" / "generated-state.json"
DOTNET = str(Path.home() / ".dotnet" / "dotnet")
if not Path(DOTNET).exists():
    DOTNET = "dotnet"

BUILD_ORDER = [
    STUBS / "UnityEngine.JSONSerializeModule" / "UnityEngine.JSONSerializeModule.csproj",
    STUBS / "UnityEngine.TextRenderingModule" / "UnityEngine.TextRenderingModule.csproj",
    STUBS / "UnityEngine.AudioModule" / "UnityEngine.AudioModule.csproj",
    STUBS / "UnityEngine.ImageConversionModule" / "UnityEngine.ImageConversionModule.csproj",
    STUBS / "UnityEngine.UIElementsModule" / "UnityEngine.UIElementsModule.csproj",
    STUBS / "UniTask" / "UniTask.csproj",
    STUBS / "Steamworks.NET" / "Steamworks.NET.csproj",
    STUBS / "Rewired_Core" / "Rewired_Core.csproj",
    STUBS / "Unity.TextMeshPro" / "Unity.TextMeshPro.csproj",
    STUBS / "Assembly-CSharp-firstpass" / "Assembly-CSharp-firstpass.csproj",
    STUBS / "Assembly-CSharp" / "Assembly-CSharp.csproj",
]

PLUGIN = ROOT / "NOVR" / "NOVR.csproj"

CS0246 = re.compile(r"error CS0246: The type or namespace name '([^']+)' could not be found")
CS0400 = re.compile(r"error CS0400: The type or namespace name '([^']+)' could not be found in the global namespace")
CS0234 = re.compile(r"error CS0234: The type or namespace name '([^']+)' does not exist in the namespace '([^']+)'")
CS1061 = re.compile(r"error CS1061: '([^']+)' does not contain a definition for '([^']+)'")
CS0117 = re.compile(r"error CS0117: '([^']+)' does not contain a definition for '([^']+)'")
CS0426 = re.compile(r"error CS0426: The type name '([^']+)' does not exist in the type '([^']+)'")
CS1729 = re.compile(r"error CS1729: '([^']+)' does not contain a constructor that takes (\d+) arguments")
CS7036 = re.compile(r"error CS7036: There is no argument given that corresponds to required parameter '([^']+)' of '([^']+)'")
CS0029 = re.compile(r"error CS0029: Cannot implicitly convert type '([^']+)' to '([^']+)'")
CS0266 = re.compile(r"error CS0266: Cannot implicitly convert type '([^']+)' to '([^']+)'")
CS1503 = re.compile(r"error CS1503: Argument (\d+): cannot convert from '([^']+)' to '([^']+)'")
CS1501 = re.compile(r"error CS1501: No overload for method '([^']+)' takes (\d+) arguments")
CS0118 = re.compile(r"error CS0118: '([^']+)' is a namespace but is used like a type")
CS1069 = re.compile(r"error CS1069: The type name '([^']+)' could not be found in the namespace '([^']+)'.*assembly '([^']+),")
CS0103 = re.compile(r"error CS0103: The name '([^']+)' does not exist in the current context")
CS0119 = re.compile(r"error CS0119: '([^']+)' is a '([^']+)', which is not valid in the given context")
CS0305 = re.compile(r"error CS0305: Using the generic type '([^']+)' requires (\d+) type arguments")
CS0308 = re.compile(r"error CS0308: The non-generic type '([^']+)' cannot be used with type arguments")
CS0702 = re.compile(r"error CS0702: Constraint cannot be special class '([^']+)'")
CS0310 = re.compile(r"error CS0310: '([^']+)' must be a non-abstract type with a public parameterless constructor")
CS0452 = re.compile(r"error CS0452: The type '([^']+)' must be a reference type")
CS0246_FILE = re.compile(r"^(.*)\((\d+),\d+\): error CS")

ENUM_LIKE = re.compile(r"(Type|Mode|Filter|Range|Order|Pole|Flags|State|Kind)$")
GAME_TYPES = {
    "Aircraft", "Airbase", "Unit", "FactionHQ", "Radar", "Pilot", "PilotDismounted",
    "TargetCam", "CameraCockpitState", "CameraOrbitState", "CameraSelectionState",
    "CameraStateManager", "CombatHUD", "FlightHud", "GameplayUI", "MessageUI",
    "StatusDisplay", "DynamicMap", "MapWaypoint", "MapIcon", "AirbaseOverlay",
    "ObjectiveOverlay", "HUDBombingState", "HUDBoresightState", "HUDTurretCrosshair",
    "HUDUnitMarker", "JammedMarker", "ThreatItem", "Turret", "FloatingOrigin",
    "GameAssets", "MissionManager", "DetailRenderer", "LobbyList", "LobbyListItem",
    "SteamWorkshop", "MapLoader", "NetworkManagerNuclearOption", "SteamLobby",
}

NS_MAP = {
    "NuclearOption": None,
    "Rewired": "Rewired",
    "TMPro": "TMPro",
    "Cysharp": "Cysharp.Threading.Tasks",
    "Steamworks": "Steamworks",
    "NaturalPoint": "NaturalPoint.TrackIR",
}


def run(cmd, cwd=None):
    env = os.environ.copy()
    env["PATH"] = str(Path.home() / ".dotnet") + os.pathsep + env.get("PATH", "")
    env["DOTNET_ROOT"] = str(Path.home() / ".dotnet")
    proc = subprocess.run(cmd, cwd=cwd or ROOT, env=env, text=True, capture_output=True)
    return proc.returncode, (proc.stdout or "") + (proc.stderr or "")


def build_stubs():
    for proj in BUILD_ORDER:
        code, out = run([DOTNET, "build", str(proj), "-c", "Release", "-v", "q", "--nologo"])
        if code != 0:
            print(f"STUB BUILD FAILED: {proj}")
            print(out[-4000:])
            return False, out
    return True, ""


def build_plugin():
    code, out = run([
        DOTNET, "build", str(PLUGIN), "-c", "Release", "-v", "q", "--nologo",
        "-p:RestoreBepInExFromNuget=true", "-p:EnableWindowsTargeting=true",
    ])
    return code == 0, out


def load_state():
    if STATE_PATH.exists():
        return json.loads(STATE_PATH.read_text())
    return {"types": {}, "enums": {}, "namespaces": []}


def save_state(state):
    STATE_PATH.write_text(json.dumps(state, indent=2, sort_keys=True))


def member_name(sig):
    m = re.search(r"([A-Za-z_][A-Za-z0-9_]*)\s*(\(|\{|;|$)", sig)
    return m.group(1) if m else sig


STATIC_TYPES = {
    "GameManager", "PlayerSettings", "GraphicsHelper", "FastMath", "LobbyPassword",
    "StringHelper", "UnitRegistry", "UnitConverter", "MissionSaveLoad", "GlobalPositionExtensions",
    "ModTypes", "ReInput",
}

STRUCT_TYPES = {"GlobalPosition"}
ENUM_TYPES = {
    "GameState", "FactionMode", "SocketType", "OrderBy", "ModType",
    "MissionPvpType", "AxisRange", "InputActionType", "ControllerElementType", "Pole",
    "ELobbyType", "TextAnchor", "FontStyle",
}
UNITY_TYPES = {
    "Component", "Object", "Transform", "GameObject", "Behaviour", "MonoBehaviour",
    "Camera", "Vector3", "Vector2", "Quaternion", "Color", "Rect", "RectTransform",
    "Material", "Texture", "Texture2D", "Sprite", "Image", "Button", "Text", "Canvas",
    "Debug", "Time", "Mathf", "Application", "Resources", "Screen", "Input",
}

NAMESPACED_TYPES = {
    "SteamWorkshop", "SteamWorkshopItem", "WorkshopMenu", "OrderBy", "Mission", "MissionTag",
    "MissionSaveLoad", "MissionSettings", "MissionKey", "MissionGroup", "MapLoader", "MapKey",
    "LobbyInstance", "LobbyList", "LobbyListItem", "HostOptions", "ModTypes", "ModType",
    "DetailSettings", "ThemeManager", "ColorTheme",
}
FOREIGN_TYPES = {
    "ActionElementMap", "ControllerMap", "InputMapper", "CanceledEventData", "ErrorEventData",
    "InputMappedEventData", "TimedOutEventData", "ConflictFoundEventData", "InputAction",
    "Controller", "Joystick", "Player", "KeyboardMap", "MouseMap", "JoystickMap",
    "ReInput", "AxisRange", "InputActionType", "ControllerElementType", "Pole",
    "TMP_Text", "TMP_Dropdown", "TextMeshProUGUI", "UniTask", "UniTaskVoid",
}

def ensure_type(state, full_name, kind="class", ns=""):
    name = full_name.split(".")[-1].split("`")[0]
    if name in UNITY_TYPES or name in FOREIGN_TYPES or name in NAMESPACED_TYPES or name in {"object", "string", "int", "float", "bool", "void", "var", "dynamic"}:
        return None
    if name in ENUM_TYPES:
        return None
    key = f"{ns}.{name}" if ns else name
    if key not in state["types"]:
        k = "struct" if name in STRUCT_TYPES else "class"
        if name in STATIC_TYPES:
            k = "static"
        state["types"][key] = {
            "ns": ns,
            "name": name,
            "kind": k,
            "members": [],
            "methods": [],
            "ctors": [],
            "nested": {},
            "base": "",
            "static": name in STATIC_TYPES,
        }
    return state["types"][key]


def load_seed_members():
    members = defaultdict(set)
    current = []
    for path in (
        STUBS / "Assembly-CSharp" / "GameTypes.cs",
        STUBS / "Assembly-CSharp" / "Namespaces.cs",
    ):
        if not path.exists():
            continue
        for raw in path.read_text().splitlines():
            line = raw.strip()
            open_m = re.search(r"\b(class|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)", line)
            if open_m:
                current.append(open_m.group(2))
            for name in re.findall(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*(\(|\{|;)", line):
                ident = name[0]
                if current and ident not in ("if", "for", "while", "switch", "catch", "using", "return"):
                    members[current[-1]].add(ident)
            if "{" in line:
                pass
            if "}" in line and current and line.count("}") >= line.count("{"):
                # pop on closing brace-only lines
                if line == "}" or line.startswith("}"):
                    current.pop()
    return members


SEED_MEMBERS = load_seed_members()


def add_member(td, kind, sig):
    if td is None:
        return False
    name = member_name(sig)
    if name in SEED_MEMBERS.get(td.get("name"), set()):
        return False
    bucket = "methods" if kind == "method" else "ctors" if kind == "ctor" else "members"
    names = {member_name(s) for s in td[bucket]}
    if name in names:
        return False
    td[bucket].append(sig)
    return True


def csharp_type(name):
    name = name.strip()
    aliases = {
        "Void": "void",
        "Boolean": "bool",
        "Int32": "int",
        "Single": "float",
        "Double": "double",
        "String": "string",
        "Object": "object",
        "Byte": "byte",
    }
    if name in aliases:
        return aliases[name]
    name = name.replace("+", ".")
    return name if name else "object"


def infer_member(src_line, member, static=False):
    line = src_line.strip()
    static_kw = "static " if static else ""
    if re.search(rf"{re.escape(member)}\s*\(\s*out\s+", line):
        return "method", f"public {static_kw}bool {member}(out object a0) {{ a0 = null; return false; }}"
    pattern = rf"(?:\w+\.)*{re.escape(member)}\s*\("
    if re.search(pattern, line):
        m = re.search(rf"{re.escape(member)}\s*\((.*)\)", line)
        nargs = 0
        if m:
            inner = m.group(1).strip()
            if inner:
                nargs = min(inner.count(",") + 1, 8)
        args = ", ".join(f"object a{i} = null" for i in range(nargs)) if nargs else ""
        ret = "object"
        if re.search(rf"if\s*\(.*{re.escape(member)}\s*\(", line):
            ret = "bool"
        prefix = line.split(member)[0][-30:] if member in line else ""
        if "await " in prefix:
            ret = "Cysharp.Threading.Tasks.UniTask"
        if re.search(rf"foreach\s*\(.*{re.escape(member)}\s*\(", line):
            ret = "System.Collections.Generic.IEnumerable<object>"
        return "method", f"public {static_kw}{ret} {member}({args}) => default;"
    if re.search(rf"{re.escape(member)}\s*\+=", line):
        return "member", f"public {static_kw}event System.Action {member};"
    if re.search(rf"{re.escape(member)}\s*=", line):
        rhs_type = "object"
        m = re.search(rf"{re.escape(member)}\s*=\s*([^;]+)", line)
        if m:
            rhs = m.group(1).strip()
            if rhs in ("true", "false"):
                rhs_type = "bool"
            elif re.fullmatch(r"-?\d+", rhs):
                rhs_type = "int"
            elif re.fullmatch(r"-?\d+\.\d+f?", rhs):
                rhs_type = "float"
            elif rhs.startswith("\""):
                rhs_type = "string"
        return "member", f"public {static_kw}{rhs_type} {member} {{ get; set; }}"
    typ = "object"
    if re.search(rf"if\s*\(.*\.{re.escape(member)}\b", line):
        typ = "bool"
    elif re.search(rf"foreach\s*\(.*\.{re.escape(member)}\b", line):
        typ = "System.Collections.Generic.IEnumerable<object>"
    return "member", f"public {static_kw}{typ} {member} {{ get; set; }}"


def read_source_line(error_line):
    m = re.match(r"^(.*)\((\d+),\d+\):", error_line)
    if not m:
        return ""
    path, ln = m.group(1), int(m.group(2))
    try:
        lines = Path(path).read_text(errors="replace").splitlines()
        if 1 <= ln <= len(lines):
            return lines[ln - 1]
    except OSError:
        return ""
    return ""


def nested_dict(kind="class"):
    return {
        "kind": kind,
        "nested": {},
        "members": [],
        "methods": [],
        "ctors": [],
        "enum_members": ["None"],
    }


def emit_nested(nname, node, indent):
    if isinstance(node, str):
        node = nested_dict("enum" if node == "enum" else "class")
    kind = node.get("kind", "class")
    if kind == "enum":
        members = node.get("enum_members") or ["None"]
        body = ",\n".join(f"{indent}    {m}" for m in members)
        return [f"{indent}public enum {nname} {{\n{body}\n{indent}}}"]
    lines = [f"{indent}public partial class {nname} {{"]
    for mem in node.get("members") or []:
        lines.append(f"{indent}    {mem}")
    for meth in node.get("methods") or []:
        lines.append(f"{indent}    {meth}")
    for ctor in node.get("ctors") or []:
        lines.append(f"{indent}    {ctor}")
    for child_name, child in (node.get("nested") or {}).items():
        lines.extend(emit_nested(child_name, child, indent + "    "))
    lines.append(f"{indent}}}")
    return lines


def emit_generated(state):
    blocks = ["// <auto-generated> compile-only stubs grown from compiler errors"]
    by_ns = defaultdict(list)
    for key, td in sorted(state["types"].items()):
        ns = td.get("ns") or ""
        if ns and ns.split(".")[0] in {t["name"] for t in state["types"].values()}:
            ns = ""
        if ns in STRUCT_TYPES or ns in ENUM_TYPES or ns in STATIC_TYPES:
            ns = ""
        by_ns[ns].append(td)

    for ns, types in sorted(by_ns.items()):
        indent = ""
        if ns:
            blocks.append(f"namespace {ns} {{")
            indent = "    "
        for td in types:
            name = td["name"]
            if td["kind"] == "enum":
                members = td.get("enum_members") or ["None"]
                body = ",\n".join(f"{indent}    {m}" for m in members)
                blocks.append(f"{indent}public enum {name} {{\n{body}\n{indent}}}")
                continue
            if td["kind"] == "ns":
                continue
            kind = td.get("kind") or "class"
            static_kw = "static " if (td.get("static") or kind == "static") else ""
            type_kw = "struct" if kind == "struct" else "class"
            lines = [f"{indent}public {static_kw}partial {type_kw} {name} {{"]
            for ctor in td["ctors"]:
                lines.append(f"{indent}    {ctor}")
            for mem in td["members"]:
                lines.append(f"{indent}    {mem}")
            for meth in td["methods"]:
                lines.append(f"{indent}    {meth}")
            for nname, node in td.get("nested", {}).items():
                lines.extend(emit_nested(nname, node, indent + "    "))
            lines.append(f"{indent}}}")
            blocks.append("\n".join(lines))
        if ns:
            blocks.append("}")
    GENERATED.write_text("\n\n".join(blocks) + "\n")


def ensure_nested_type(state, parent_full, nested_name, kind="class"):
    parts = parent_full.replace("+", ".").split(".")
    root_name = parts[0]
    td = ensure_type(state, root_name, ns="")
    if td is None:
        return False
    current_nested = td.setdefault("nested", {})
    for part in parts[1:]:
        node = current_nested.get(part)
        if not isinstance(node, dict):
            node = nested_dict("class" if node != "enum" else "enum")
            current_nested[part] = node
        current_nested = node.setdefault("nested", {})
    existing = current_nested.get(nested_name)
    if not isinstance(existing, dict):
        current_nested[nested_name] = nested_dict(kind)
        return True
    return False


def parse_errors(output):
    errors = []
    for line in output.splitlines():
        if ": error CS" in line:
            errors.append(line)
    return errors


def apply_errors(state, errors):
    changed = False
    for line in errors:
        src = read_source_line(line)

        m = CS0234.search(line)
        if m:
            name, ns = m.group(1), m.group(2)
            if ns.startswith("UnityEngine"):
                continue
            if ensure_type(state, f"{ns}.{name}", ns=ns, kind="class") is not None:
                changed = True
            continue

        m = CS0246.search(line) or CS0400.search(line)
        if m:
            name = m.group(1)
            if name in ("NuclearOption", "Rewired", "TMPro", "Cysharp", "Steamworks", "NaturalPoint", "UnityEngine"):
                # namespace-only; add a dummy type inside later via CS0234
                if name not in state["namespaces"]:
                    state["namespaces"].append(name)
                    ensure_type(state, f"{name}.Placeholder", ns=name, kind="class")
                    changed = True
                continue
            kind = "enum" if ENUM_LIKE.search(name) else "class"
            ensure_type(state, name, kind=kind, ns="")
            changed = True
            continue

        m = CS0426.search(line)
        if m:
            nested, parent = m.group(1), m.group(2)
            kind = "enum" if ENUM_LIKE.search(nested) else "class"
            if ensure_nested_type(state, parent, nested, kind):
                changed = True
            continue

        m = CS1061.search(line) or CS0117.search(line)
        if m:
            if typ in UNITY_TYPES or typ in {"object", "string", "int", "float", "bool"}:
                continue
            real_ns_prefixes = ("NuclearOption", "Rewired", "TMPro", "Cysharp", "Steamworks", "UnityEngine", "NaturalPoint")
            if any(typ.startswith(p + ".") or typ == p for p in real_ns_prefixes):
                ns = ".".join(typ.split(".")[:-1])
                simple = typ.split(".")[-1]
                td = ensure_type(state, simple, ns=ns)
            elif "." in typ:
                parent = ".".join(typ.split(".")[:-1])
                nested_name = typ.split(".")[-1]
                root = ensure_type(state, parent.split(".")[0], ns="")
                if root is None:
                    continue
                current_nested = root.setdefault("nested", {})
                current = root
                for part in parent.split(".")[1:] + [nested_name]:
                    n = current_nested.get(part)
                    if not isinstance(n, dict):
                        n = nested_dict("class")
                        current_nested[part] = n
                    current = n
                    current_nested = n.setdefault("nested", {})
                kind, sig = infer_member(src, member, static=False)
                bucket = "methods" if kind == "method" else "members"
                names = {member_name(s) for s in current.get(bucket, [])}
                if member_name(sig) not in names:
                    current.setdefault(bucket, []).append(sig)
                    changed = True
                continue
            else:
                td = ensure_type(state, typ, ns="")
            if td is None:
                continue
            kind, sig = infer_member(src, member, static=td.get("static") or td.get("kind") == "static")
            if add_member(td, kind, sig):
                changed = True
            continue

        m = CS1729.search(line)
        if m:
            typ, n = m.group(1), int(m.group(2))
            simple = typ.split(".")[-1]
            td = ensure_type(state, simple)
            args = ", ".join(f"object a{i} = null" for i in range(int(n)))
            if td is not None and add_member(td, "ctor", f"public {simple}({args}) : this() {{}}"):
                changed = True
            continue

        m = CS1501.search(line)
        if m:
            method, n = m.group(1), int(m.group(2))
            tm = re.search(r"([A-Za-z_][A-Za-z0-9_\.]*)\." + re.escape(method), src)
            type_name = tm.group(1).split(".")[-1] if tm else "GameManager"
            td = ensure_type(state, type_name)
            if td is None:
                continue
            args = ", ".join(f"object a{i} = null" for i in range(int(n)))
            static_kw = "static " if td.get("static") or td.get("kind") == "static" else ""
            if add_member(td, "method", f"public {static_kw}object {method}({args}) => default;"):
                changed = True
            continue

        m = CS0103.search(line)
        if m:
            name = m.group(1)
            if name in UNITY_TYPES or name == "XRSettings":
                continue
            if ensure_type(state, name) is not None:
                changed = True
            continue

        m = CS0029.search(line) or CS0266.search(line)
        if m:
            # not easily mapped; skip - next CS1061 often follows
            continue

        m = CS1069.search(line)
        if m:
            # extra unity module already created
            continue

    return changed


def main():
    max_iter = int(sys.argv[1]) if len(sys.argv) > 1 else 25
    global SEED_MEMBERS
    SEED_MEMBERS = load_seed_members()
    state = load_state()
    emit_generated(state)

    ok, out = build_stubs()
    if not ok:
        sys.exit(1)

    for i in range(1, max_iter + 1):
        ok, out = build_plugin()
        errors = parse_errors(out)
        err_count = len(errors)
        print(f"=== plugin build iter {i}: {'OK' if ok else f'{err_count} errors'} ===")
        if ok:
            print("PLUGIN BUILD SUCCEEDED")
            return 0
        codes = defaultdict(int)
        for e in errors:
            mm = re.search(r"error (CS\d+)", e)
            if mm:
                codes[mm.group(1)] += 1
        print(" codes:", dict(sorted(codes.items(), key=lambda kv: -kv[1])[:12]))
        sample = [e.split("error ")[-1][:180] for e in errors[:8]]
        for s in sample:
            print("  ", s)

        changed = apply_errors(state, errors)
        if not changed:
            print("No stub mutations from errors; stopping.")
            GENERATED.write_text(GENERATED.read_text() if GENERATED.exists() else "")
            save_state(state)
            Path("/tmp/plugin-build.log").write_text(out)
            return 1
        save_state(state)
        emit_generated(state)
        ok, stout = build_stubs()
        if not ok:
            print("Stub rebuild failed after generation")
            print(stout[-3000:])
            Path("/tmp/stub-build.log").write_text(stout)
            return 1

    print("Hit max iterations")
    return 1


if __name__ == "__main__":
    sys.exit(main())
