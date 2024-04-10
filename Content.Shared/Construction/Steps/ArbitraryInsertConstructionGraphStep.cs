using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    public abstract partial class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("name")]
        public string PrName { get; private set; } = string.Empty;

        public string Name => Loc
            .GetString(PrName) //Yep.. That's terrifying..
            .Replace("Pipe", "Труба")
            .Replace("carpet", "ковёр")
            .Replace("blue", "синий")
            .Replace("pink", "розовый")
            .Replace("red", "красный")
            .Replace("cyan", "голубой")
            .Replace("purple", "фиолетовый")
            .Replace("white", "белый")
            .Replace("black", "чёрный")
            .Replace("orange", "оранжевый")
            .Replace("green", "зелёный");

        [DataField("icon")] public SpriteSpecifier? Icon { get; private set; }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            examinedEvent.PushMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", Name)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[] { ("name", Name) },
                Icon = Icon,
            };
        }
    }
}
