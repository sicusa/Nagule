namespace Nagule.Examples;

using System.Numerics;

using ImGuiNET;

public static class ProfilerUI
{
    public record struct ProfileEntry(string Name, Profile Profile);

    public struct State : ISingletonComponent
    {
        public bool IsOpen;
        public float Timer = float.PositiveInfinity;
        public bool StopProfile;
        public Dictionary<string, ProfileEntry[]> ProfileMap = new();
        public float[] FramerateSamplePoints = new float[100];

        public State() {}
    }

    public static void Show(IContext context, float updateInterval)
    {
        ref var state = ref context.AcquireAny<State>(out bool exists);
        state.Timer += context.DeltaTime;

        const ImGuiWindowFlags WindowFlags =
            ImGuiWindowFlags.MenuBar;

        if (!ImGui.Begin("Profiler", ref state.IsOpen, WindowFlags)) {
            return;
        }

        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("Settings")) {
                if (ImGui.MenuItem("Stop Profile", null, state.StopProfile)) {
                    state.StopProfile = !state.StopProfile;
                }
                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }

        if (!state.StopProfile && state.Timer > updateInterval) {
            state.Timer = 0;
            UpdateProfiles(context, ref state);
        }

        var contentSize = ImGui.GetContentRegionAvail();
        var framerate = ImGui.GetIO().Framerate;

        var frameratePoints = state.FramerateSamplePoints;
        Array.Copy(frameratePoints, 1, frameratePoints, 0, frameratePoints.Length - 1);
        frameratePoints[frameratePoints.Length - 1] = framerate;
        ImGui.PlotLines(
            "##plot", ref frameratePoints[0], frameratePoints.Length,
            0, ((int)framerate).ToString(), 0, 60, new Vector2(contentSize.X, 64));

        ShowProfiles(context, ref state, "Load");
        ShowProfiles(context, ref state, "FrameStart");
        ShowProfiles(context, ref state, "Update");
        ShowProfiles(context, ref state, "EngineUpdate");
        ShowProfiles(context, ref state, "LateUpdate");
        ShowProfiles(context, ref state, "UpdateCommands");
        ShowProfiles(context, ref state, "ResourceCommands");
        ShowProfiles(context, ref state, "RenderCommands");
        ShowProfiles(context, ref state, "CompositionCommands");
        ShowProfiles(context, ref state, "RenderPipeline_0");
        ShowProfiles(context, ref state, "CompositionPipeline_0");

        ImGui.End();
    }

    private static void UpdateProfiles(IContext context, ref State state)
    {
        var profileMap = state.ProfileMap;
        var profileGroups = context.Profiles
            .Select(p => (p.Key.IndexOf('/'), p.Key, p.Value))
            .GroupBy(
                p => p.Key.Substring(0, p.Item1),
                p => new ProfileEntry(p.Key.Substring(p.Item1 + 1), p.Value));
        
        profileMap.Clear();
        foreach (var group in profileGroups) {
            profileMap.Add(group.Key, group.ToArray());
        }
    }

    private unsafe static void ShowProfiles(IContext context, ref State state, string category)
    {
        if (!state.ProfileMap.TryGetValue(category, out var profiles)
                || !ImGui.CollapsingHeader(category)) {
            return;
        }

        ImGui.PushID(category);

        const ImGuiTableFlags TableFlags =
            ImGuiTableFlags.Borders
            | ImGuiTableFlags.SortMulti
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.ScrollX
            | ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.Sortable;

        var contentSize = ImGui.GetContentRegionAvail();
        if (ImGui.BeginTable("##table", 6, TableFlags, new Vector2(contentSize.X, 256f))) {
            const ImGuiTableColumnFlags ColumnFlags = ImGuiTableColumnFlags.PreferSortDescending;
            ImGui.TableSetupColumn("Name", ColumnFlags);
            ImGui.TableSetupColumn("Current", ColumnFlags | ImGuiTableColumnFlags.DefaultSort);
            ImGui.TableSetupColumn("Averange", ColumnFlags);
            ImGui.TableSetupColumn("Maximum", ColumnFlags);
            ImGui.TableSetupColumn("Minimum", ColumnFlags);
            ImGui.TableSetupColumn("Frame", ColumnFlags);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            var sortSpecs = ImGui.TableGetSortSpecs();
            if (state.Timer == 0 || sortSpecs.SpecsDirty) {
                var columnSortSpecs = (ImGuiTableColumnSortSpecs*)sortSpecs.Specs;
                Comparison<ProfileEntry>? comparison = null;

                for (int i = 0; i < sortSpecs.SpecsCount; ++i) {
                    ref var spec = ref columnSortSpecs[i];
                    Comparison<ProfileEntry>? newComparison = null;
                    if (spec.SortDirection == ImGuiSortDirection.Ascending) {
                        newComparison = spec.ColumnIndex switch {
                            0 => ProfileAscendingComparison(e => e.Name),
                            1 => ProfileAscendingComparison(e => e.Profile.CurrentElapsedTime),
                            2 => ProfileAscendingComparison(e => e.Profile.AverangeElapsedTime),
                            3 => ProfileAscendingComparison(e => e.Profile.MaximumElapsedTime),
                            4 => ProfileAscendingComparison(e => e.Profile.MinimumElapsedTime),
                            5 => ProfileAscendingComparison(e => e.Profile.CurrentFrame),
                            _ => null
                        };
                    }
                    else {
                        newComparison = spec.ColumnIndex switch {
                            0 => ProfileDescendingComparison(e => e.Name),
                            1 => ProfileDescendingComparison(e => e.Profile.CurrentElapsedTime),
                            2 => ProfileDescendingComparison(e => e.Profile.AverangeElapsedTime),
                            3 => ProfileDescendingComparison(e => e.Profile.MaximumElapsedTime),
                            4 => ProfileDescendingComparison(e => e.Profile.MinimumElapsedTime),
                            5 => ProfileDescendingComparison(e => e.Profile.CurrentFrame),
                            _ => null
                        };
                    }
                    if (newComparison != null) {
                        comparison = comparison == null
                            ? newComparison : CombineProfileComparison(comparison, newComparison);
                    }
                }

                if (comparison != null) {
                    profiles.AsSpan().Sort(comparison);
                }
                sortSpecs.SpecsDirty = false;
            }

            foreach (var (layer, profile) in profiles) {
                ImGui.PushStyleColor(ImGuiCol.Text,
                    profile.CurrentElapsedTime > 0.01f
                        ? new Vector4(255, 0, 0, 255)
                        : new Vector4(0, 255, 0, 255));
                ImGui.TableNextColumn(); ImGui.Text(layer);
                ImGui.PopStyleColor();

                ImGui.TableNextColumn(); ImGui.Text(profile.CurrentElapsedTime.ToString());
                ImGui.TableNextColumn(); ImGui.Text(profile.AverangeElapsedTime.ToString());
                ImGui.TableNextColumn(); ImGui.Text(profile.MaximumElapsedTime.ToString());
                ImGui.TableNextColumn(); ImGui.Text(profile.MinimumElapsedTime.ToString());
                ImGui.TableNextColumn(); ImGui.Text(profile.CurrentFrame.ToString());
            }
            ImGui.EndTable();
        }

        ImGui.PopID();
    }

    private static Comparison<ProfileEntry> CombineProfileComparison(Comparison<ProfileEntry> c1, Comparison<ProfileEntry> c2)
        => (e1, e2) => {
            var r = c1(e1, e2);
            return r != 0 ? r : c2(e1, e2);
        };

    private static Comparison<ProfileEntry> ProfileAscendingComparison<T>(Func<ProfileEntry, T> keySelector)
        where T : IComparable
        => (e1, e2) => keySelector(e1).CompareTo(keySelector(e2));

    private static Comparison<ProfileEntry> ProfileDescendingComparison<T>(Func<ProfileEntry, T> keySelector)
        where T : IComparable
        => (e1, e2) => keySelector(e2).CompareTo(keySelector(e1));
}