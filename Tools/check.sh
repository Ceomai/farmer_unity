#!/usr/bin/env bash
# Unity kurulu olmayan makinede C# kodunu UnityEngine taslaklarına karşı derleyerek tür/sözdizimi denetimi yapar.
# Kullanım: bash Tools/check.sh            → tüm Assets/Scripts
#           bash Tools/check.sh dosya1.cs …  → yalnızca Core + verilen dosyalar (diğer modüllere ait eksik adlar hata verir)
cd "$(dirname "$0")/.."
CSC="/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csc.exe"
REF="C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2"
NJ="C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/NuGet/Newtonsoft.Json.dll"
OUT="${TMPDIR:-/tmp}/ciftlik_check_$$.dll"
if [ $# -gt 0 ]; then FILES=$(ls Assets/Scripts/Core/*.cs; for f in "$@"; do echo "$f"; done); else FILES=$(find Assets/Scripts -name '*.cs' -not -path '*/Editor/*'); fi
WFILES=$(for f in $FILES Tools/stubs/*.cs; do cygpath -w "$f"; done)
"$CSC" -nologo -noconfig -nostdlib -target:library -langversion:9 -nowarn:0162,0168,0169,0219,0414,0649,0659,0660,0661,0067,1998,0108,0114,0618,0472,8632 \
  -out:"$(cygpath -w "$OUT")" -r:"$REF/mscorlib.dll" -r:"$REF/System.dll" -r:"$REF/System.Core.dll" -r:"$REF/System.Xml.dll" -r:"$REF/System.Runtime.Serialization.dll" -r:"$REF/Facades/netstandard.dll" -r:"$NJ" \
  $WFILES 2>&1 | sed -E 's#^.*farmer[-_]unity[\\/]##'
RC=${PIPESTATUS[0]}; rm -f "$OUT"; exit $RC
