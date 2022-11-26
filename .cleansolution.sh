#!/bin/bash
echo "Removing the following directories recursively:"
echo src/DDS.Avalonia{,.Android,.Desktop,.iOS,.Mobile,.Web}/{obj,bin} 
rm -r src/DDS.Avalonia{,.Android,.Desktop,.iOS,.Mobile,.Web}/{obj,bin}
dotnet restore DDS.sln
