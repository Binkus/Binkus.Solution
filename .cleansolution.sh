#!/bin/bash
echo "Removing the following directories recursively:"
echo src/DDS{,.Android,.Desktop,.iOS,.Mobile,.Web}/{obj,bin} 
rm -r src/DDS{,.Android,.Desktop,.iOS,.Mobile,.Web}/{obj,bin}
dotnet restore DDS.sln
