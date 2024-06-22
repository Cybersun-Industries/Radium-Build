using System.Linq;
using System.Numerics;
using Content.Client.Body.Systems;
using Content.Client.Guidebook.Richtext;
using Content.Client.UserInterface.Controls;
using Content.Shared.Body.Part;
using Content.Shared.Radium.Medical.Surgery.Prototypes;
using Content.Shared.Traits.Assorted;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Radium.Medical.Surgery.UI;

public sealed class SurgeryMenu : DefaultWindow
{
    //Fucking hell...

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] private readonly EntityManager _entityManager = default!;

    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public TextureRect Surface = new()
    {
        TexturePath = "/Textures/Radium/Interface/Surgery/nanotrasen.png",
        Stretch = TextureRect.StretchMode.Scale
    };

    public TextureButton DollContainer = new();
    public TextureRect Bkg = new();
    public Box ListBox = new();
    public ItemList SurgeryOptions = new();
    public HSpacer Spacer = new();

    private static SurgeryBoundUserInterface? _bui;

    public bool Dirty = true;
    public string CurrentSelection = "Nothing";

    public HashSet<SurgeryOperationPrototype>
        HeadOperationsList = new(),
        MouthOperationsList = new(),
        EyesOperationsList = new(),
        ArmsOperationsList = new(),
        BodyOperationsList = new(),
        LegsOperationsList = new();

    public Dictionary<int, string>
        HeadEventIndexes = new(),
        MouthEventIndexes = new(),
        EyesEventIndexes = new(),
        ArmsEventIndexes = new(),
        BodyEventIndexes = new(),
        LegsEventIndexes = new();

    public NetEntity Uid;

    public SurgeryMenu(SurgeryBoundUserInterface? bui, EntityUid entityUid)
    {
        Title = Loc.GetString("surgery-window-title");
        IoCManager.InjectDependencies(this);
        Uid = _entityManager.GetNetEntity(entityUid);
        foreach (var surgeryOperationPrototype in _prototypeManager.EnumeratePrototypes<SurgeryOperationPrototype>())
        {
            switch (surgeryOperationPrototype.BodyPart)
            {
                case "Head":
                    HeadOperationsList.Add(surgeryOperationPrototype);
                    break;
                case "Mouth":
                    MouthOperationsList.Add(surgeryOperationPrototype);
                    break;
                case "Eyes":
                    EyesOperationsList.Add(surgeryOperationPrototype);
                    break;
                case "Arm":
                    ArmsOperationsList.Add(surgeryOperationPrototype);
                    break;
                case "Torso":
                    BodyOperationsList.Add(surgeryOperationPrototype);
                    break;
                case "Leg":
                    LegsOperationsList.Add(surgeryOperationPrototype);
                    break;
            }
        }

        _bui = bui;
        Resizable = false;

        Contents.VerticalAlignment = VAlignment.Top;
        Contents.SetWidth = 430;
        Contents.SetHeight = 300;
        Surface.SetWidth = 430;
        Surface.SetHeight = 300;
        Surface.Stretch = TextureRect.StretchMode.KeepCentered;
        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_deactivated.png";
        DollContainer.SetSize = new Vector2(128, 128);
        DollContainer.HorizontalAlignment = HAlignment.Left;
        DollContainer.VerticalAlignment = VAlignment.Top;
        DollContainer.OnButtonUp += SelectPart;

        Spacer.SetWidth = 80;

        Bkg.SetSize = new Vector2(128, 128);
        Bkg.HorizontalAlignment = HAlignment.Left;
        Bkg.VerticalAlignment = VAlignment.Top;
        Bkg.Stretch = TextureRect.StretchMode.Scale;
        Bkg.TexturePath = "/Textures/Radium/Interface/Surgery/template.png";
        SurgeryOptions.SetWidth = 300;
        SurgeryOptions.Margin = new Thickness(0, 5);
        SurgeryOptions.ItemSeparation = 2;
        SurgeryOptions.SelectMode = ItemList.ItemListSelectMode.Button;
        SurgeryOptions.AddStyleClass("lumaclass");
        Bkg.AddChild(DollContainer);
        Surface.AddChild(Bkg);
        Surface.AddChild(Spacer);
        ListBox.Align = BoxContainer.AlignMode.End;
        ListBox.AddChild(SurgeryOptions);
        Surface.AddChild(ListBox);
        Contents.AddChild(Surface);
        SurgeryOptions.OnItemSelected += OnItemSelected;
        SurgeryOptions.OnItemDeselected += OnItemDeselected;
    }

    private void OnItemDeselected(ItemList.ItemListDeselectedEventArgs obj)
    {
        Close();
    }

    private void OnItemSelected(ItemList.ItemListSelectedEventArgs obj)
    {
        string? operation = null;
        var symmetry = BodyPartSymmetry.None;
        switch (CurrentSelection)
        {
            case "Head":
                if (!HeadEventIndexes.TryGetValue(obj.ItemIndex, out var head))
                {
                    break;
                }

                operation = head;
                break;
            case "Mouth":
                if (!MouthEventIndexes.TryGetValue(obj.ItemIndex, out var mouth))
                {
                    break;
                }

                operation = mouth;
                break;
            case "Eyes":
                if (!EyesEventIndexes.TryGetValue(obj.ItemIndex, out var eyes))
                {
                    break;
                }

                operation = eyes;
                break;
            case "LArm":
                if (!ArmsEventIndexes.TryGetValue(obj.ItemIndex, out var arml))
                {
                    break;
                }

                symmetry = BodyPartSymmetry.Left;
                operation = arml;
                break;
            case "RArm":
                if (!ArmsEventIndexes.TryGetValue(obj.ItemIndex, out var armr))
                {
                    break;
                }

                symmetry = BodyPartSymmetry.Right;
                operation = armr;
                break;
            case "Torso":
                if (!BodyEventIndexes.TryGetValue(obj.ItemIndex, out var body))
                {
                    break;
                }

                operation = body;
                break;
            case "LLeg":
                if (!LegsEventIndexes.TryGetValue(obj.ItemIndex, out var llegs))
                {
                    break;
                }

                symmetry = BodyPartSymmetry.Left;
                operation = llegs;
                break;
            case "RLeg":
                if (!LegsEventIndexes.TryGetValue(obj.ItemIndex, out var rlegs))
                {
                    break;
                }

                symmetry = BodyPartSymmetry.Right;
                operation = rlegs;
                break;
        }

        if (operation == null)
            return;
        _bui!.BeginSurgery(Uid, operation, symmetry);
    }

    private void SelectPart(BaseButton.ButtonEventArgs obj)
    {
        var position = obj.Event.RelativePosition;

        var x = position[0];
        var y = position[1];

        switch (y)
        {
            case >= 10 and <= 41:
                switch (y)
                {
                    case >= 21 and <= 26 when x is >= 52 and <= 57 or >= 62 and <= 67:
                        //eyes
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_eyes.png";
                        CurrentSelection = "Eyes";
                        break;
                    case >= 32 and <= 37 when x is >= 60 and <= 62:
                        //mouth
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_mouth.png";
                        CurrentSelection = "Mouth";
                        break;
                    default:
                        //head
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_head.png";
                        CurrentSelection = "Head";
                        break;
                }

                break;
            case >= 42 and <= 80:
            {
                switch (x)
                {
                    case >= 25 and <= 43:
                        //right arm
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_arm_right.png";
                        CurrentSelection = "RArm";
                        break;
                    case >= 44 and <= 80:
                        //body
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_chest.png";
                        CurrentSelection = "Torso";
                        break;
                    case >= 81 and <= 98:
                        //left arm
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_arm_left.png";
                        CurrentSelection = "LArm";
                        break;
                }

                break;
            }
            case >= 81 and <= 126:
            {
                switch (x)
                {
                    case >= 36 and <= 57:
                        //left leg
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_leg_left.png";
                        CurrentSelection = "RLeg";
                        break;
                    case >= 63 and <= 88:
                        //right leg
                        DollContainer.TexturePath = "/Textures/Radium/Interface/Surgery/surgery_leg_right.png";
                        CurrentSelection = "LLeg";
                        break;
                }

                break;
            }
        }

        Dirty = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!Dirty)
            return;

        Dirty = false;
        SurgeryOptions.Clear();
        HeadEventIndexes.Clear();
        EyesEventIndexes.Clear();
        MouthEventIndexes.Clear();
        BodyEventIndexes.Clear();
        ArmsEventIndexes.Clear();
        LegsEventIndexes.Clear();

        UpdateSurgeryList();
    }

    public void UpdateSurgeryList()
    {
        var bodySystem = _entitySystemManager.GetEntitySystem<BodySystem>();
        int index;
        switch (CurrentSelection)
        {
            case "Head":
                index = 0;
                foreach (var operation in HeadOperationsList.Where(operation => !operation.IsHidden))
                {
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    HeadEventIndexes.Add(index, operation.ID);
                    index++;
                }

                break;
            case "Mouth":
                index = 0;
                foreach (var operation in MouthOperationsList.Where(operation => !operation.IsHidden))
                {
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    MouthEventIndexes.Add(index, operation.ID);
                    index++;
                }

                break;
            case "Eyes":
                if (_entityManager.HasComponent<PermanentBlindnessComponent>(_entityManager.GetEntity(Uid)))
                {
                    var operation = _prototypeManager.Index<SurgeryOperationPrototype>("EyeCureOperation");
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    EyesEventIndexes.Add(0, operation.ID);
                }
                else
                {
                    index = 0;
                    foreach (var operation in EyesOperationsList.Where(operation => !operation.IsHidden))
                    {
                        SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                        {
                            Text = operation.LocalizedName,
                            TooltipText = operation.LocalizedDescription,
                            TooltipEnabled = true,
                        });
                        EyesEventIndexes.Add(index, operation.ID);
                        index++;
                    }
                }

                break;
            case "RArm":
                var parts3 = bodySystem.GetBodyChildren(_entityManager.GetEntity(Uid)).ToList();
                var rArm = parts3.Where(i =>
                        i.Component.Symmetry == BodyPartSymmetry.Right && i.Component.PartType == BodyPartType.Arm)
                    .ToList();
                if (rArm.Count == 0)
                {
                    SurgeryOptions.Clear();
                    ArmsEventIndexes.Clear();
                    var operation = _prototypeManager.Index<SurgeryOperationPrototype>("AddAOperation");
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    ArmsEventIndexes.Add(0, operation.ID);
                }
                else
                {
                    index = 0;
                    foreach (var operation in ArmsOperationsList.Where(operation => !operation.IsHidden))
                    {
                        SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                        {
                            Text = operation.LocalizedName,
                            TooltipText = operation.LocalizedDescription,
                            TooltipEnabled = true,
                        });
                        ArmsEventIndexes.Add(index, operation.ID);
                        index++;
                    }
                }

                break;
            case "LArm":
                var parts2 = bodySystem.GetBodyChildren(_entityManager.GetEntity(Uid)).ToList();
                var lArm = parts2.Where(i =>
                        i.Component.Symmetry == BodyPartSymmetry.Left && i.Component.PartType == BodyPartType.Arm)
                    .ToList();
                if (lArm.Count == 0)
                {
                    SurgeryOptions.Clear();
                    ArmsEventIndexes.Clear();
                    var operation = _prototypeManager.Index<SurgeryOperationPrototype>("AddAOperation");
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    ArmsEventIndexes.Add(0, operation.ID);
                }
                else
                {
                    index = 0;
                    foreach (var operation in ArmsOperationsList.Where(operation => !operation.IsHidden))
                    {
                        SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                        {
                            Text = operation.LocalizedName,
                            TooltipText = operation.LocalizedDescription,
                            TooltipEnabled = true,
                        });
                        ArmsEventIndexes.Add(index, operation.ID);
                        index++;
                    }
                }

                break;
            case "Torso":
                index = 0;
                foreach (var operation in BodyOperationsList.Where(operation => !operation.IsHidden))
                {
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    BodyEventIndexes.Add(index, operation.ID);
                    index++;
                }

                break;
            case "RLeg":
                var parts1 = bodySystem.GetBodyChildren(_entityManager.GetEntity(Uid)).ToList();
                var rLeg = parts1.Where(i =>
                        i.Component.Symmetry == BodyPartSymmetry.Right && i.Component.PartType == BodyPartType.Leg)
                    .ToList();
                if (rLeg.Count == 0)
                {
                    SurgeryOptions.Clear();
                    LegsEventIndexes.Clear();
                    var operation = _prototypeManager.Index<SurgeryOperationPrototype>("AddLOperation");
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    LegsEventIndexes.Add(0, operation.ID);
                }
                else
                {
                    index = 0;
                    foreach (var operation in LegsOperationsList.Where(operation => !operation.IsHidden))
                    {
                        SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                        {
                            Text = operation.LocalizedName,
                            TooltipText = operation.LocalizedDescription,
                            TooltipEnabled = true,
                        });
                        LegsEventIndexes.Add(index, operation.ID);
                        index++;
                    }
                }

                break;
            case "LLeg":

                var parts = bodySystem.GetBodyChildren(_entityManager.GetEntity(Uid)).ToList();
                var lLeg = parts.Where(i =>
                    i.Component.Symmetry == BodyPartSymmetry.Left && i.Component.PartType == BodyPartType.Leg).ToList();
                if (lLeg.Count == 0)
                {
                    SurgeryOptions.Clear();
                    LegsEventIndexes.Clear();
                    var operation = _prototypeManager.Index<SurgeryOperationPrototype>("AddLOperation");
                    SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                    {
                        Text = operation.LocalizedName,
                        TooltipText = operation.LocalizedDescription,
                        TooltipEnabled = true,
                    });
                    LegsEventIndexes.Add(0, operation.ID);
                }
                else
                {
                    index = 0;
                    foreach (var operation in LegsOperationsList.Where(operation => !operation.IsHidden))
                    {
                        SurgeryOptions.Add(new ItemList.Item(SurgeryOptions)
                        {
                            Text = operation.LocalizedName,
                            TooltipText = operation.LocalizedDescription,
                            TooltipEnabled = true,
                        });
                        LegsEventIndexes.Add(index, operation.ID);
                        index++;
                    }
                }

                break;
        }
    }
}
