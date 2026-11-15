using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RockScatterSettings))]
public sealed class RockScatterSettingsEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        root.Add(new HelpBox("Tweak settings, then Rebuild Rocks. Changes are baked, not live. Rebuilding replaces this field's generated prefab; terrain and gameplay are not edited.", HelpBoxMessageType.Info));
        Section(root, "References", "terrain", "rover", "definitions");
        Section(root, "Population density", "seed", "useDensity", "decorationDensity", "obstacleDensity");
        var summary = new Label();
        root.Add(summary);
        void RefreshSummary()
        {
            var settings = (RockScatterSettings)target;
            try
            {
                summary.text = $"{settings.AreaHectares:F2} ha • {settings.DecorationCount:N0} decorations • {settings.ObstacleCount:N0} obstacles • {settings.samples.count:N0} passive samples";
            }
            catch (InvalidOperationException error) { summary.text = error.Message; }
            root.Q<PropertyField>("decorationDensity").SetEnabled(settings.useDensity);
            root.Q<PropertyField>("obstacleDensity").SetEnabled(settings.useDensity);
            root.Q<PropertyField>("decoration-count").SetEnabled(!settings.useDensity);
            root.Q<PropertyField>("obstacles-count").SetEnabled(!settings.useDensity);
        }
        Population(root, "Decorations (no collision)", "decoration");
        Population(root, "Obstacles (convex collision)", "obstacles");
        Population(root, "Passive sample candidates", "samples");
        Section(root, "Distribution", "fieldScale", "clustering", "maximumSlope", "minimumGap");
        var burial = Section(root, "Ground contact");
        Field(burial, "burialFraction", "Minimum Burial Fraction");
        Field(burial, "maximumBurialFraction", "Maximum Burial Fraction");
        Field(burial, "groundAlignment");
        Section(root, "Clearance and access screening", "spawnClearance", "roverClearanceRadius",
            "settledRoverHeight", "candidateApproachRange", "drivingSlopeLimit", "routeGridMeters",
            "starterSampleRadius", "starterSamples", "studySites", "exclusions");
        var result = new HelpBox("", HelpBoxMessageType.Info);
        result.visible = false;
        var rebuild = new Button(() =>
        {
            serializedObject.ApplyModifiedProperties();
            try
            {
                var field = RockFieldBaker.Bake((RockScatterSettings)target);
                result.text = $"Rebuilt {field.decorativeCount:N0} decorations, {field.obstacleCount:N0} obstacles and {field.sampleCount:N0} passive samples. Save the scene to keep the new links.";
                result.messageType = HelpBoxMessageType.Info;
            }
            catch (Exception error)
            {
                result.text = error.Message;
                result.messageType = HelpBoxMessageType.Error;
                Debug.LogException(error, target);
            }
            result.visible = true;
        }) { text = "Rebuild Rocks" };
        root.Add(rebuild);
        root.Add(result);
        root.TrackSerializedObjectValue(serializedObject, _ => RefreshSummary());
        root.schedule.Execute(() =>
        {
            rebuild.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }).Every(250);
        RefreshSummary();
        return root;
    }

    private Foldout Section(VisualElement root, string title, params string[] properties)
    {
        var section = new Foldout { text = title, value = true };
        root.Add(section);
        foreach (string property in properties) Field(section, property);
        return section;
    }

    private void Field(VisualElement parent, string property, string label = null)
    {
        parent.Add(new PropertyField(serializedObject.FindProperty(property), label) { name = property });
    }

    private void Population(VisualElement root, string title, string property)
    {
        var section = Section(root, title);
        var population = serializedObject.FindProperty(property);
        foreach (string child in new[] { "count", "minimumDiameter", "maximumDiameter", "cullDistance" })
            section.Add(new PropertyField(population.FindPropertyRelative(child)) { name = property + "-" + child });
    }
}
