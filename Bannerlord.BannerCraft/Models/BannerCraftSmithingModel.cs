﻿using System;
using System.Collections.Generic;
using Bannerlord.BannerCraft.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static TaleWorlds.Core.ArmorComponent;

namespace Bannerlord.BannerCraft.Models
{
    public class BannerCraftSmithingModel : SmithingModel
    {
        private SmithingModel _model;

        public BannerCraftSmithingModel(SmithingModel model)
        {
            _model = model;
        }

        public override int GetCraftingPartDifficulty(CraftingPiece craftingPiece) => _model.GetCraftingPartDifficulty(craftingPiece);

        public override int CalculateWeaponDesignDifficulty(WeaponDesign weaponDesign) => _model.CalculateWeaponDesignDifficulty(weaponDesign);

        public override int GetModifierTierForSmithedWeapon(WeaponDesign weaponDesign, Hero weaponsmith) => _model.GetModifierTierForSmithedWeapon(weaponDesign, weaponsmith);

        public override Crafting.OverrideData GetModifierChanges(int modifierTier, Hero hero, WeaponComponentData weapon) => _model.GetModifierChanges(modifierTier, hero, weapon);

        public override IEnumerable<Crafting.RefiningFormula> GetRefiningFormulas(Hero weaponsmith) => _model.GetRefiningFormulas(weaponsmith);

        public override ItemObject GetCraftingMaterialItem(CraftingMaterials craftingMaterial) => _model.GetCraftingMaterialItem(craftingMaterial);

        public override int GetSkillXpForRefining(ref Crafting.RefiningFormula refineFormula) => _model.GetSkillXpForRefining(ref refineFormula);

        public override int GetSkillXpForSmelting(ItemObject item) => _model.GetSkillXpForSmelting(item);

        public override int GetSkillXpForSmithingInFreeBuildMode(ItemObject item) => _model.GetSkillXpForSmithingInFreeBuildMode(item);

        public override int GetSkillXpForSmithingInCraftingOrderMode(ItemObject item) => _model.GetSkillXpForSmithingInCraftingOrderMode(item);

        public override int[] GetSmithingCostsForWeaponDesign(WeaponDesign weaponDesign) => _model.GetSmithingCostsForWeaponDesign(weaponDesign);

        public override int GetEnergyCostForRefining(ref Crafting.RefiningFormula refineFormula, Hero hero) => _model.GetEnergyCostForRefining(ref refineFormula, hero);

        public override int GetEnergyCostForSmithing(ItemObject item, Hero hero) => _model.GetEnergyCostForSmithing(item, hero);

        public override int GetEnergyCostForSmelting(ItemObject item, Hero hero) => _model.GetEnergyCostForSmelting(item, hero);

        public override float ResearchPointsNeedForNewPart(int totalPartCount, int openedPartCount) => _model.ResearchPointsNeedForNewPart(totalPartCount, openedPartCount);

        public override int GetPartResearchGainForSmeltingItem(ItemObject item, Hero hero) => _model.GetEnergyCostForSmelting(item, hero);

        public override int GetPartResearchGainForSmithingItem(ItemObject item, Hero hero, bool isFreeBuildMode) => _model.GetPartResearchGainForSmithingItem(item, hero, isFreeBuildMode);

        public override int[] GetSmeltingOutputForItem(ItemObject item)
        {
            var result = _model.GetSmeltingOutputForItem(item);

            if (item == null)
            {
                return result;
            }

            if (Settings.Instance.DefaultSmeltingModel)
            {
                return result;
            }

            var metalCount = 0;
            for (var i = 0; i < result.Length; i++)
            {
                if (i is >= 2 and <= 6)
                {
                    metalCount += result[i];
                }
            }

            if (ArmorCraftingVM.ItemTypeIsWeapon(ArmorCraftingVM.GetItemType(item)))
            {
                var metalCap = GetMetalMax(item.WeaponComponent.PrimaryWeapon.WeaponClass);
                if (metalCount > 0 && metalCap > 0)
                {
                    while (metalCount > metalCap)
                    {
                        for (var i = 0; i < result.Length; i++)
                        {
                            if (i is >= 2 and <= 6 && result[i] > 0 && metalCount > metalCap)
                            {
                                result[i]--;
                                metalCount--;
                            }
                        }
                    }
                }

                if (item.WeaponComponent.PrimaryWeapon.WeaponClass == WeaponClass.Dagger)
                {
                    result[7] = 0;
                }

                if (result[1] == 0 && metalCap > 0)
                {
                    result[1]++;
                }
            }
            else
            {
                int[] cost = GetCraftingInputForArmor(item);

                for (int i = (int)CraftingMaterials.Iron6; i >= (int)CraftingMaterials.Iron3; i--)
                {
                    if (cost[i] < 0)
                    {
                        result[i - 1] -= (int)(cost[i] * 0.3f);
                        result[i - 2] -= (int)(cost[i] * 0.5f);
                    }
                }
                result[(int)CraftingMaterials.Charcoal] = -(int)Math.Max(Math.Ceiling(item.Weight * 0.75 / GetCraftingMaterialItem(CraftingMaterials.Charcoal).Weight), 1f);
            }

            return result;
        }

        public int GetMetalMax(WeaponClass weaponClass) => weaponClass switch
        {
            WeaponClass.Dagger => 1,
            WeaponClass.ThrowingAxe => 1,
            WeaponClass.ThrowingKnife => 1,
            WeaponClass.Crossbow => 1,
            WeaponClass.SmallShield => 1,

            WeaponClass.OneHandedSword => 2,
            WeaponClass.LowGripPolearm => 2,
            WeaponClass.OneHandedPolearm => 2,
            WeaponClass.TwoHandedPolearm => 2,
            WeaponClass.OneHandedAxe => 2,
            WeaponClass.Mace => 2,
            WeaponClass.LargeShield => 2,
            WeaponClass.Pick => 2,

            WeaponClass.TwoHandedAxe => 3,
            WeaponClass.TwoHandedMace => 3,
            WeaponClass.TwoHandedSword => 3,
            _ => -1
        };

        public float CalculateBotchingChance(Hero hero, int difficulty)
        {
            float chance = 0.01f * (difficulty - hero.GetSkillValue(DefaultSkills.Crafting));

            return MBMath.ClampFloat(chance, 0f, Settings.Instance.MaximumBotchChance);
        }

        public int CalculateArmorDifficulty(ItemObject item)
        {
            float result = item.Tierf * 20f;

            ItemType itemType = ArmorCraftingVM.GetItemType(item);
            if (itemType == ItemType.Invalid)
            {
                /*
				 * Vanilla crafting item, should not get here, but let's be nice
				 */
                if (item.WeaponDesign == null)
                {
                    return 0;
                }
                return CalculateWeaponDesignDifficulty(item.WeaponDesign);
            }

            switch (itemType)
            {
                case ItemType.Barding:

                case ItemType.HeadArmor:
                case ItemType.ShoulderArmor:
                case ItemType.BodyArmor:
                case ItemType.ArmArmor:
                case ItemType.LegArmor:
                    switch (itemType)
                    {
                        case ItemType.Barding:
                        case ItemType.BodyArmor:
                            result *= 1.5f;
                            break;

                        case ItemType.HeadArmor:
                            result *= 1.2f;
                            break;
                    }

                    switch (item.ArmorComponent.MaterialType)
                    {
                        case ArmorMaterialTypes.Cloth:
                            result *= 1f;
                            break;

                        case ArmorMaterialTypes.Leather:
                            result *= 1.1f;
                            break;

                        case ArmorMaterialTypes.Chainmail:
                            result *= 1.25f;
                            break;

                        case ArmorMaterialTypes.Plate:
                            result *= 1.4f;
                            break;
                    }
                    break;

                case ItemType.Shield:
                    /*
					 * result * item.Tierf / 6f is an arbitrary attempt to balance out the difficulty of
					 * these so they're approximately on par with equivalent tier melee weapons
					 */
                    result += item.WeaponComponent.PrimaryWeapon.MaxDataValue / 10f;
                    result += result * item.Tierf / 6f;
                    break;

                case ItemType.Bow:
                case ItemType.Crossbow:
                    result += result * item.Tierf / 6f;
                    break;

                case ItemType.Arrows:
                case ItemType.Bolts:
                    result += item.WeaponComponent.PrimaryWeapon.MaxDataValue * item.WeaponComponent.PrimaryWeapon.MissileDamage;
                    result += result * item.Tierf / 6f;
                    break;

                case ItemType.Banner:
                    result += result * item.Tierf / 4f;
                    break;

                case ItemType.OneHandedWeapon:
                case ItemType.TwoHandedWeapon:
                case ItemType.Polearm:
                case ItemType.Thrown:
                    result += result * item.Tierf / 3f;
                    break;
            }

            return MBMath.ClampInt((int)result, 10, 300);
        }

        public int[] GetCraftingInputForArmor(ItemObject item)
        {
            /*
			 * [0,9) are vanilla materials
			 * [9,13) are extra materials
			 * ie indices 0 through 8 inclusive are vanilla, indices 9 through 12 inclusive are extra
			 * Calculate extra indices as CraftingMaterials.NumCraftingMats + ExtraCraftingMaterials.{item}
			 */
            const int numMaterials = (int)CraftingMaterials.NumCraftingMats + (int)ExtraCraftingMaterials.NumExtraCraftingMats;
            var result = new int[numMaterials];

            if (item == null)
            {
                return result;
            }

            ItemType itemType = ArmorCraftingVM.GetItemType(item);
            if (itemType == ItemType.Invalid)
            {
                /*
                 * Vanilla crafting item, should not get here
				 */

                return result;
            }

            /*
			 * Get the materials used based on tier
			 */
            ExtraCraftingMaterials clothMaterial = item.Tierf switch
            {
                < 4f => ExtraCraftingMaterials.Linen,
                _ => ExtraCraftingMaterials.Velvet
            };

            ExtraCraftingMaterials leatherMaterial = item.Tierf switch
            {
                < 4f => ExtraCraftingMaterials.Fur,
                _ => ExtraCraftingMaterials.Leather
            };

            CraftingMaterials metalMaterial = item.Tierf switch
            {
                < 4f => CraftingMaterials.Iron3,
                < 5f => CraftingMaterials.Iron4,
                < 6f => CraftingMaterials.Iron5,
                _ => CraftingMaterials.Iron6
            };

            CraftingMaterials woodMaterial = CraftingMaterials.Wood;

            /*
			 * An item low in its tier uses fewer high tier metals
			 */
            float tierf = (float)(item.Tierf - Math.Truncate(item.Tierf));
            float highMetalRatio;
            float midMetalRatio;
            float lowMetalRatio;
            if (tierf > 0.5f)
            {
                highMetalRatio = 0.8f;
                midMetalRatio = 0.2f;
                lowMetalRatio = 0.0f;
            }
            else
            {
                highMetalRatio = 0.6f;
                midMetalRatio = 0.3f;
                lowMetalRatio = 0.1f;
            }

            int woodIndex = (int)CraftingMaterials.Wood;

            float weightTotal = item.Weight * 1.2f;

            switch (itemType)
            {
                case ItemType.Barding:

                case ItemType.HeadArmor:
                case ItemType.ShoulderArmor:
                case ItemType.BodyArmor:
                case ItemType.ArmArmor:
                case ItemType.LegArmor:
                    float metalRatio = 0f;
                    float clothRatio = 0f;
                    float leatherRatio = 0f;
                    switch (item.ArmorComponent.MaterialType)
                    {
                        case ArmorMaterialTypes.Plate:
                        case ArmorMaterialTypes.Chainmail:
                            metalRatio = 0.8f;
                            clothRatio = 1f - metalRatio;

                            break;

                        case ArmorMaterialTypes.Cloth:
                            metalRatio = item.Tierf switch
                            {
                                < 2f => 0.1f,
                                < 4f => 0.2f,
                                _ => 0.3f
                            };
                            clothRatio = 1f - metalRatio;

                            metalMaterial = (CraftingMaterials)((int)metalMaterial - 1);

                            break;

                        case ArmorMaterialTypes.Leather:
                            metalRatio = item.Tierf switch
                            {
                                < 2f => 0.15f,
                                < 4f => 0.3f,
                                _ => 0.4f
                            };
                            leatherRatio = 1f - metalRatio;

                            metalMaterial = (CraftingMaterials)((int)metalMaterial - 1);

                            break;
                    }

                    int highMetalIndex = (int)metalMaterial;
                    int midMetalIndex = highMetalIndex - 1;
                    int lowMetalIndex = highMetalIndex - 2;

                    int clothIndex = (int)CraftingMaterials.NumCraftingMats + (int)clothMaterial;
                    int leatherIndex = (int)CraftingMaterials.NumCraftingMats + (int)leatherMaterial;

                    int numMetal = (int)Math.Ceiling(weightTotal * metalRatio / GetCraftingMaterialItem(metalMaterial).Weight);
                    int numCloth = clothRatio > 0f ? Math.Max((int)Math.Ceiling(weightTotal * clothRatio / GetCraftingMaterialItem(clothMaterial).Weight), 1) : 0;
                    int numLeather = leatherRatio > 0f ? Math.Max((int)Math.Ceiling(weightTotal * leatherRatio / GetCraftingMaterialItem(leatherMaterial).Weight), 1) : 0;

                    int numHighMetal = (int)Math.Ceiling(numMetal * highMetalRatio);
                    int numMidMetal = (int)Math.Ceiling(numMetal * midMetalRatio);
                    int numLowMetal = (int)Math.Ceiling(numMetal * lowMetalRatio);

                    result[highMetalIndex] = -numHighMetal;
                    result[midMetalIndex] = -numMidMetal;
                    result[lowMetalIndex] = -numLowMetal;
                    result[leatherIndex] = -numLeather;
                    result[clothIndex] = -numCloth;

                    break;

                case ItemType.Shield:
                case ItemType.Bow:
                case ItemType.Crossbow:
                case ItemType.Arrows:
                case ItemType.Bolts:
                    metalRatio = item.WeaponComponent.PrimaryWeapon.PhysicsMaterial switch
                    {
                        "shield_metal" => 0.8f,
                        _ => 0.3f
                    };

                    metalRatio = itemType switch
                    {
                        ItemType.Bow => 0.8f,
                        ItemType.Crossbow => 1f,
                        ItemType.Arrows => 0.4f,
                        ItemType.Bolts => 0.4f,
                        _ => metalRatio
                    };

                    weightTotal = itemType switch
                    {
                        ItemType.Bow => weightTotal * 4f,
                        ItemType.Crossbow => weightTotal * 4f,
                        ItemType.Arrows => weightTotal * item.PrimaryWeapon.MaxDataValue * 4f,
                        ItemType.Bolts => weightTotal * item.PrimaryWeapon.MaxDataValue * 4f,
                        _ => weightTotal
                    };

                    float woodRatio = 1f - metalRatio;

                    highMetalIndex = (int)metalMaterial;
                    midMetalIndex = highMetalIndex - 1;
                    lowMetalIndex = highMetalIndex - 2;

                    numMetal = (int)Math.Max(Math.Ceiling(weightTotal * metalRatio / GetCraftingMaterialItem(metalMaterial).Weight), 1);
                    int numWood = (int)Math.Max(Math.Ceiling(weightTotal * woodRatio / GetCraftingMaterialItem(woodMaterial).Weight), 1);

                    numHighMetal = (int)Math.Ceiling(numMetal * highMetalRatio);
                    numMidMetal = (int)Math.Ceiling(numMetal * midMetalRatio);
                    numLowMetal = (int)Math.Ceiling(numMetal * lowMetalRatio);

                    result[highMetalIndex] = -numHighMetal;
                    result[midMetalIndex] = -numMidMetal;
                    result[lowMetalIndex] = -numLowMetal;
                    result[woodIndex] = -numWood;

                    break;

                case ItemType.Banner:
                    weightTotal *= 2f;

                    clothMaterial = ExtraCraftingMaterials.Velvet;

                    metalRatio = 0.8f;
                    clothRatio = (1f - metalRatio) / 2f;
                    woodRatio = (1f - metalRatio) / 2f;

                    clothIndex = (int)CraftingMaterials.NumCraftingMats + (int)clothMaterial;
                    highMetalIndex = (int)metalMaterial;

                    numCloth = (int)Math.Max(Math.Ceiling(weightTotal * clothRatio / GetCraftingMaterialItem(clothMaterial).Weight), 1);
                    numWood = (int)Math.Max(Math.Ceiling(weightTotal * woodRatio / GetCraftingMaterialItem(woodMaterial).Weight), 1);
                    numMetal = (int)Math.Max(Math.Ceiling(weightTotal * metalRatio / GetCraftingMaterialItem(metalMaterial).Weight), 1);

                    result[highMetalIndex] = -numMetal;
                    result[woodIndex] = -numWood;
                    result[clothIndex] = -numCloth;

                    break;

                case ItemType.OneHandedWeapon:
                case ItemType.TwoHandedWeapon:
                case ItemType.Polearm:
                case ItemType.Thrown:
                    /*
					 * Let's not do anything fancy
					 */
                    if (item != null && item.WeaponDesign != null)
                    {
                        var result2 = GetSmithingCostsForWeaponDesign(item.WeaponDesign);
                        for (int i = 0; i < result2.Length; i++)
                        {
                            result[i] = result2[i];
                        }
                    }
                    break;
            }

            return result;
        }

        public ItemObject GetCraftingMaterialItem(ExtraCraftingMaterials craftingMaterial)
        {
            return Game.Current.ObjectManager.GetObject<ItemObject>(craftingMaterial.ToString().ToLower()); ;
        }

        public int GetEnergyCostForArmor(ItemObject item, Hero hero)
        {
            float result = 0;

            result += item.Weight * 5f;
            result += item.Tierf * 5f;

            ItemType itemType = ArmorCraftingVM.GetItemType(item);

            result += itemType switch
            {
                ItemType.Barding => 70f,

                ItemType.HeadArmor => 30f,
                ItemType.ShoulderArmor => 40f,
                ItemType.BodyArmor => 50f,
                ItemType.ArmArmor => 10f,
                ItemType.LegArmor => 10f,

                ItemType.Shield => 20f,

                ItemType.Bow => 40f,
                ItemType.Crossbow => 40f,

                ItemType.Arrows => 1f * item.WeaponComponent.PrimaryWeapon.MaxDataValue,
                ItemType.Bolts => 1f * item.WeaponComponent.PrimaryWeapon.MaxDataValue,
                _ => 0f
            };

            if (item.HasArmorComponent)
            {
                result += item.ArmorComponent.MaterialType switch
                {
                    ArmorMaterialTypes.Plate => 40f,
                    ArmorMaterialTypes.Chainmail => 25f,
                    _ => 10f
                };
            }

            if (hero.GetPerkValue(DefaultPerks.Crafting.PracticalSmith))
            {
                result = (result + 1) / 2;
            }

            return MBMath.ClampInt((int)result, 10, 300);
        }

        public int GetModifierTierForItem(ItemObject item, Hero hero)
        {
            /*
			 * 25 is just the default value from MCM
			 *
			 * Items have 4 modifiers, 2 good, 2 bad
			 * Negative modifier is neutral and gives the item as is
			 * Modifiers 0 and 1 give the bad ones and are achievable from anywhere up to 25 skill over difficulty
			 * Modifiers 2, 3, and 4 give the good ones and are achievable from skill equals difficulty
			 */
            int skillValue = hero.CharacterObject.GetSkillValue(DefaultSkills.Crafting);
            int itemDifficulty = CalculateArmorDifficulty(item);

            int skillFloor = Settings.Instance.SkillOverDifficultyBeforeNoPenalty;

            /*
			 * randomInt becomes between -50 and 50 with default settings
			 */
            int randomInt = MBRandom.RandomInt(0, 100) - skillFloor;
            int difference = skillValue - itemDifficulty;
            if (difference + randomInt < 0)
            {
                return GetModifierTierPenaltyForLowSkill(difference, randomInt);
            }

            float legendarySmithChance = hero.GetPerkValue(DefaultPerks.Crafting.LegendarySmith) ? DefaultPerks.Crafting.LegendarySmith.PrimaryBonus + Math.Max(0, hero.GetSkillValue(DefaultSkills.Crafting) - 300) * 0.01f : 0f;
            float masterSmithChance = hero.GetPerkValue(DefaultPerks.Crafting.MasterSmith) ? DefaultPerks.Crafting.MasterSmith.PrimaryBonus : 0f;
            float experiencedSmithChance = hero.GetPerkValue(DefaultPerks.Crafting.ExperiencedSmith) ? DefaultPerks.Crafting.ExperiencedSmith.PrimaryBonus : 0f;

            /*
			 * And now change randomInt back to between 0 and 100, then convert to between 0 and 1
			 */
            float randomFloat = (randomInt + skillFloor) * 0.01f;
            if (randomFloat < legendarySmithChance)
            {
                /*
				 * Just pick the highest tier thing available
				 * This makes sure we work with things like Large Bag of Balanced which has a price_factor of 1.8
				 * Our legendary things have a price factor of 2.5 and the masterwork vanilla items have a price factor of 1.5
				 * So our numbers still work
				 */
                return 10;
            }

            if (randomFloat < legendarySmithChance + masterSmithChance)
            {
                return 3;
            }

            if (randomFloat < legendarySmithChance + masterSmithChance + experiencedSmithChance)
            {
                return 2;
            }

            return -1;
        }

        private int GetModifierTierPenaltyForLowSkill(int difference, int randomInt)
        {
            if (difference >= 0)
            {
                /*
				 * Sanity check
				 */
                return 0;
            }

            /*
			 * Difference is a value between -300 and -1
			 * RandomInt is a number between -25 and 75
			 * Make randomInt between 0 and 100
			 */
            randomInt += Settings.Instance.SkillOverDifficultyBeforeNoPenalty;
            /*
			 * Which means this is now between -200 and 99
			 */
            int f = difference + randomInt;
            /*
			 * So our modifier should be anywhere from -1 to 1
			 */
            return f switch
            {
                < -100 => 0,
                < 0 => 1,
                _ => -1
            };
        }
    }
}