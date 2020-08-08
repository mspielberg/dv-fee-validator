using UnityModManagerNet;

namespace DvMod.CustomFeeValidator
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Enable logging")] public bool isLoggingEnabled =
#if DEBUG
            true;
#else
            false;
#endif
        [Draw(Label = "Fee Validation Type", Type = DrawType.ToggleGroup, Vertical = true)] public FeeValidationType selectedValidationType = FeeValidationType.LastLoco;

        override public void Save(UnityModManager.ModEntry entry)
        {
            Save<Settings>(this, entry);
        }

        public void OnChange() { }
    }
}
