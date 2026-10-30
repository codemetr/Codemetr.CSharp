# Validation guide

Run dotnet restore --locked-mode, dotnet build -c Release --no-restore and
`dotnet test -c Release --no-build`, then `bash scripts/check-metrics.sh`.
The candidate behaviour is exercised through regression tests, not installed from
local package feeds. After an authorised publication, analyse examples/member-size
with the matching published tool, inspect memberHotspots/codeLines/statementCount,
and check that opting into the documented rules yields exit 1 for the long member.
