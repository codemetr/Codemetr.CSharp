# Data model

MemberMetrics: existing fields plus non-negative int CodeLines/StatementCount.
GuardRules: nullable MaxMemberCodeLines/MaxMemberStatements, unset means inherit
in an override and disabled in the effective rules.
MemberHotspot: Type, Member, Kind, File, Line, CodeLines, StatementCount,
CyclomaticComplexity, CognitiveComplexity, NestingDepth, ParameterCount.
CodemetrReport: additive MemberHotspots list, independent of Types inclusion.
