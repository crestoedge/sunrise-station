   public sealed class DynamicAppearanceComponent : Component
   {
       public MarkingSet MarkingSet { get; set; } = new MarkingSet();
       public string Species { get; set; }
       public string BodyType { get; set; }
       public Sex Sex { get; set; }
       public int SkinColor { get; set; }
       public Dictionary<string, BaseLayerInfo> CustomBaseLayers { get; set; } = new();
   }