using Sandbox;
using Sandbox.Internal;
using System.Collections.Generic;
using TerrorTown;

namespace TTT_Classes
{
    public partial class GoldComponent : EntityComponent<ModelEntity>
    {
        public bool TimerActive { get; set; } = false;
        public RealTimeUntil Timer { get; set; } = 5;
        public Material GoldMaterial { get; set; }
        public float? OriginalSpeed { get; set; } = null;
        public GoldComponent() 
        {
            GoldMaterial = Material.Load("materials/complex_gold.vmat");
        }
        public void Init()
        {
            if (Entity is TerrorTown.Player ply && ply.MovementController is TerrorTown.WalkController walker && OriginalSpeed == null)
            {
                OriginalSpeed = walker.SpeedMultiplier;
                walker.SpeedMultiplier = 0.15f;
            }
            Entity.SetMaterialOverride(GoldMaterial);
            foreach (Entity child in Entity.Children)
            {
                if (child is ModelEntity modelChild)
                {
                    modelChild.SetMaterialOverride(GoldMaterial);
                }
            }
            ResetTimer(active: false);
        }

        public void ResetTimer(int time = 5, bool active = true)
        {
            if (Entity is TerrorTown.Player)
            {
                Timer = time;
            } else
            {
                Timer = 30;
            }
            TimerActive = active;
        }

        [GameEvent.Tick.Server]
        public void TickServer()
        {
            Game.AssertServer();
            if (Timer && TimerActive) 
            {
                RemoveGold();
            }
            
        }

        [GameEvent.Tick.Client]
        public void TickClient()
        {
            if (Entity is TerrorTown.Player ply)
            {
                if (ply.Inventory.ActiveChild is Carriable carriable)
                {
                    carriable.ViewModelEntity?.SetMaterialOverride(GoldMaterial);
                }
            }
        }

        public void RemoveGold()
        {
            Entity.ClearMaterialOverride();
            foreach (Entity child in Entity.Children)
            {
                if (child is ModelEntity modelChild)
                {
                    modelChild.ClearMaterialOverride();
                }
            }
            if (Entity is TerrorTown.Player ply)
            {
                ResetViewModel(ply);
                if (ply.MovementController is TerrorTown.WalkController walker)
                {
                    walker.SpeedMultiplier += (float) OriginalSpeed - 0.15f;
                }
            }
            Entity.Components.Remove(this);
        }

        [ClientRpc]
        public static void ResetViewModel(TerrorTown.Player ply)
        {
            if (ply != Game.LocalPawn) return;
            if (ply.Inventory.ActiveChild is Carriable carriable)
            {
                carriable.ViewModelEntity?.ClearMaterialOverride();
            }
        }
    }
    public partial class Midas : TTT_Class
    {
        public override string Name { get; set; } = "Midas";
        public override string Description { get; set; } = "A blessing or a curse? Everything you touch turns into gold.";
        public override float Frequency { get; set; } = 1f;
        public override Color Color { get; set; } = Color.FromRgb(0xFFD700);

        public override void RoundStartAbility()
        {
            FootprintComponent footprints =  Entity.Components.GetOrCreate<FootprintComponent>().Init();
            footprints.LastPostion = Entity.Position;
            footprints.FootprintColor = Color.White;
            footprints.LeftDecalDefinition = GlobalGameNamespace.ResourceLibrary.Get<DecalDefinition>("decals/footstep_left.decal");
            footprints.RightDecalDefinition = GlobalGameNamespace.ResourceLibrary.Get<DecalDefinition>("decals/footstep_right.decal");
            DrawClothes(Entity);
        }

        [ClientRpc]
        public static void RedrawLegs(TerrorTown.Player ply)
        {
            if (Game.LocalPawn != ply) return;
            
            // Delete PlayerLegs
            ply.PlayerLegs.Delete();

            // Get and apply new first person clothes
            List<Clothing> clothings = GetClothing(true);
            foreach (Clothing clothing in clothings)
            {
                ply.Clothing.Toggle(clothing);
            }
            ply.Clothing.DressEntity(ply);

            // Create new PlayerLegs
            ply.CreateLegs();

            // Disable all other clothes in the new PlayerLegs
            foreach (Entity child in new List<Entity>(ply.PlayerLegs.Children))
            {
                if (child is ModelEntity modelChild)
                {
                    string name = modelChild.GetModelName();
                    if (name != "models/cosmetics/kingoutfit/hair_crown.vmdl" &&
                        name != "models/cosmetics/kingoutfit/king_coat.vmdl" &&
                        name != "models/cosmetics/kingoutfit/king_leggings.vmdl" &&
                        name != "models/cosmetics/kingoutfit/king_shoes.vmdl")
                    {
                        modelChild.EnableDrawing = false;
                    }
                }
            }
        }

        public static void DrawClothes(TerrorTown.Player ply)
        {
            foreach (Clothing clothing in new List<Clothing>(ply.Clothing.Clothing))
            {
                ply.Clothing.Toggle(clothing);
            }

            List<Clothing> clothings = GetClothing();
            foreach (Clothing clothing in clothings)
            {
                ply.Clothing.Toggle(clothing);
            }
            ply.Clothing.DressEntity(ply);
            RedrawLegs(ply);
        }

        public static List<Clothing> GetClothing(bool client = false)
        {

            Clothing crown = new Clothing();
            crown.Model = "models/cosmetics/kingoutfit/hair_crown.vmdl";
            crown.SlotsOver = Clothing.Slots.Face | Clothing.Slots.HeadTop | Clothing.Slots.HeadBottom;
            crown.Category = Clothing.ClothingCategory.Hat;

            Clothing coat = new Clothing();
            coat.Model = "models/cosmetics/kingoutfit/king_coat.vmdl";
            coat.SlotsOver = Clothing.Slots.Chest;
            coat.Category = Clothing.ClothingCategory.Tops;
            if (client)
            {
                coat.HideBody = Clothing.BodyGroups.Chest;
            }

            Clothing leggings = new Clothing();
            leggings.Model = "models/cosmetics/kingoutfit/king_leggings.vmdl";
            leggings.SlotsOver = Clothing.Slots.Waist;
            leggings.Category = Clothing.ClothingCategory.Bottoms;
            leggings.HideBody = Clothing.BodyGroups.Legs;

            Clothing shoes = new Clothing();
            shoes.Model = "models/cosmetics/kingoutfit/king_shoes.vmdl";
            shoes.SlotsOver = Clothing.Slots.LeftFoot | Clothing.Slots.RightFoot;
            shoes.Category = Clothing.ClothingCategory.Footwear;
            shoes.HideBody = Clothing.BodyGroups.Feet;

            List<Clothing> clothingList = new()
            {
                crown,
                coat,
                leggings,
                shoes
            };

            return clothingList;
        }

        [Event("Player.StartTouch")]
        public void StartTouch(Entity other, TerrorTown.Player ply)
        {
            if (ply != Entity) return; 
            if (other is ModelEntity model)
            {
                model.Components.GetOrCreate<GoldComponent>().Init();
            }
        }

        [Event("Player.EndTouch")]
        public void EndTouch(Entity other, TerrorTown.Player ply)
        {
            if (ply != Entity) return;
            if (other is ModelEntity model)
            {
                model.Components.TryGet(out GoldComponent component);
                component?.ResetTimer();
            }
        }

    }
}