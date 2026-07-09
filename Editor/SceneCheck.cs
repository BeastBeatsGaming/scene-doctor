using System.Collections.Generic;

namespace SaiMayank.SceneDoctor
{
    /// <summary>
    /// Base class for every diagnostic rule.
    ///
    /// To add a new check, derive from this class anywhere in an Editor assembly
    /// that references SaiMayank.SceneDoctor.Editor. Scene Doctor finds it
    /// automatically via TypeCache \u2014 no registration list to edit.
    /// </summary>
    public abstract class SceneCheck
    {
        // Stable, unique id. Used as the enable/disable preference key.
        public abstract string Id { get; }

        // Human-readable name shown in the Checks settings list.
        public abstract string DisplayName { get; }

        // Grouping label, e.g. "Scene", "UI", "Physics", "Code".
        public virtual string Category => "Scene";

        // Short sentence describing what the check looks for.
        public virtual string Description => string.Empty;

        // Whether this check runs by default (some are noisier than others).
        public virtual bool EnabledByDefault => true;

        // Produce zero or more diagnostics for the given scene snapshot.
        public abstract IEnumerable<Diagnostic> Run(SceneContext context);
    }
}
