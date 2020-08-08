using System.ComponentModel;

namespace DvMod.CustomFeeValidator
{
    public enum FeeValidationType
    {
        [Description("Ignore consumables fees from all locomotives coupled to the last entered locomotive")]
        LastLoco,
        [Description("Ignore all fees from all locomotives that have not yet despawned")]
        ExistingLocos,
    }
}
