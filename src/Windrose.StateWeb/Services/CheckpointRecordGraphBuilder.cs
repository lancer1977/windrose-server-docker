using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Services;

public static class CheckpointRecordGraphBuilder
{
    public static SaveRecordGraphReport Build(IReadOnlyList<CheckpointEntrySummary> entries, string? sourcePath)
    {
        var entrySummaries = new List<SaveRecordGraphEntrySummary>();
        var recordTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var identityMarkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var portableMarkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var referenceMarkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var coLocatedEvidence = new List<string>();

        foreach (var entry in entries)
        {
            var recordTypeSet = new HashSet<string>(entry.RecordTypes, StringComparer.OrdinalIgnoreCase);
            var identitySet = new HashSet<string>(entry.IdentityMarkers, StringComparer.OrdinalIgnoreCase);
            var portableSet = new HashSet<string>(entry.CandidatePortableMarkers, StringComparer.OrdinalIgnoreCase);
            var referenceSet = new HashSet<string>(entry.ReferenceMarkers, StringComparer.OrdinalIgnoreCase);

            foreach (var token in recordTypeSet) recordTypes.Add(token);
            foreach (var token in identitySet) identityMarkers.Add(token);
            foreach (var token in portableSet) portableMarkers.Add(token);
            foreach (var token in referenceSet) referenceMarkers.Add(token);

            if (identitySet.Count > 0 && portableSet.Count > 0)
            {
                coLocatedEvidence.Add(entry.Path);
            }

            entrySummaries.Add(new SaveRecordGraphEntrySummary
            {
                Path = entry.Path,
                Kind = entry.Kind,
                Classification = entry.Classification,
                RecordTypes = recordTypeSet.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                IdentityMarkers = identitySet.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                CandidatePortableMarkers = portableSet.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                ReferenceMarkers = referenceSet.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                Notes = entry.Notes
            });
        }

        var hasCrossLinkedData = coLocatedEvidence.Count > 0;
        var canExportWithoutRekey = !hasCrossLinkedData && identityMarkers.Count > 0 && portableMarkers.Count > 0 && referenceMarkers.Count > 0;
        var verdict = hasCrossLinkedData
            ? "unsafe: identity and portable markers co-reside in the same SST entries; export/import would require explicit rekey rules"
            : identityMarkers.Count == 0 && portableMarkers.Count == 0
                ? "inconclusive: no player graph markers were discovered in the checkpoint sample"
                : "inconclusive: player graph markers were discovered, but rekey rules were not proven";

        return new SaveRecordGraphReport
        {
            ReadOnly = true,
            SourcePath = sourcePath,
            HasCrossLinkedIdentityAndPortableData = hasCrossLinkedData,
            CanExportWithoutRekey = canExportWithoutRekey,
            Verdict = verdict,
            RecordTypes = recordTypes.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            IdentityMarkers = identityMarkers.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            CandidatePortableMarkers = portableMarkers.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            ReferenceMarkers = referenceMarkers.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            CoLocatedEvidence = coLocatedEvidence.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            Entries = entrySummaries.OrderByDescending(entry => entry.Path, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }
}
