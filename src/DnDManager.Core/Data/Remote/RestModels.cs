using System;
using System.Text.Json.Serialization;

namespace DnDManager.Data.Remote
{
    public class RestCampaign
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("notes")] public string Notes { get; set; } = "";
        [JsonPropertyName("story")] public string Story { get; set; } = "";
    }

    public class RestEquipment
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("cost")] public string Cost { get; set; } = "";
        [JsonPropertyName("weight")] public string Weight { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("image_url")] public string ImageUrl { get; set; } = "";
    }

    public class RestSpell
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("level")] public int Level { get; set; }
        [JsonPropertyName("school")] public string School { get; set; } = "";
        [JsonPropertyName("classes")] public string Classes { get; set; } = "";
        [JsonPropertyName("components")] public string Components { get; set; } = "";
        [JsonPropertyName("casting_time")] public string CastingTime { get; set; } = "";
        [JsonPropertyName("range")] public string Range { get; set; } = "";
        [JsonPropertyName("duration")] public string Duration { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("image_url")] public string ImageUrl { get; set; } = "";
    }

    public class RestCharacterEquipment
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("character_id")] public long CharacterId { get; set; }
        [JsonPropertyName("equipment_id")] public long EquipmentId { get; set; }
        [JsonPropertyName("quantity")] public int Quantity { get; set; } = 1;
    }

    public class RestCharacterSpell
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("character_id")] public long CharacterId { get; set; }
        [JsonPropertyName("spell_id")] public long SpellId { get; set; }
    }

    public class RestBattleState
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("campaign_id")] public long CampaignId { get; set; }
        [JsonPropertyName("map_url")] public string? MapUrl { get; set; }
        [JsonPropertyName("cols")] public int Cols { get; set; } = 24;
        [JsonPropertyName("rows")] public int Rows { get; set; } = 16;
        [JsonPropertyName("walls_json")] public string WallsJson { get; set; } = "[]";
        [JsonPropertyName("fog_json")] public string FogJson { get; set; } = "[]";
    }

    public class RestBattleCombatant
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("campaign_id")] public long CampaignId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("initiative")] public int Initiative { get; set; }
        [JsonPropertyName("hp")] public int Hp { get; set; }
        [JsonPropertyName("is_current")] public bool IsCurrent { get; set; }
        [JsonPropertyName("portrait_url")] public string PortraitUrl { get; set; } = "";
        [JsonPropertyName("character_id")] public long? CharacterId { get; set; }
        [JsonPropertyName("grid_x")] public int GridX { get; set; } = -1;
        [JsonPropertyName("grid_y")] public int GridY { get; set; } = -1;
        [JsonPropertyName("sort_order")] public int SortOrder { get; set; }
    }

    public class RestQuest
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("campaign_id")] public long CampaignId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("status")] public string Status { get; set; } = "In corso";
        [JsonPropertyName("is_locked")] public bool IsLocked { get; set; }
        [JsonPropertyName("linked_quest_id")] public long? LinkedQuestId { get; set; }
    }

    public class RestQuestNote
    {
        [JsonPropertyName("quest_id")] public long QuestId { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; } = "";
    }

    public class RestProfile
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; } = "";
    }

    public class RestCampaignMember
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("campaign_id")] public long CampaignId { get; set; }
        [JsonPropertyName("user_id")] public Guid UserId { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; } = "player";
    }

    public class RestCharacter
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("race")] public string Race { get; set; } = "";
        [JsonPropertyName("class")] public string Class { get; set; } = "";
        [JsonPropertyName("level")] public int Level { get; set; } = 1;
        [JsonPropertyName("notes")] public string Notes { get; set; } = "";
        [JsonPropertyName("sheet_url")] public string SheetUrl { get; set; } = "";
        [JsonPropertyName("campaign_id")] public long? CampaignId { get; set; }
        [JsonPropertyName("is_npc")] public bool IsNpc { get; set; }
        [JsonPropertyName("portrait_url")] public string PortraitUrl { get; set; } = "";
        [JsonPropertyName("owner_id")] public Guid? OwnerId { get; set; }
        [JsonPropertyName("str")] public int Str { get; set; } = 10;
        [JsonPropertyName("dex")] public int Dex { get; set; } = 10;
        [JsonPropertyName("con")] public int Con { get; set; } = 10;
        [JsonPropertyName("int_score")] public int IntScore { get; set; } = 10;
        [JsonPropertyName("wis")] public int Wis { get; set; } = 10;
        [JsonPropertyName("cha")] public int Cha { get; set; } = 10;
        [JsonPropertyName("proficient_saves")] public string ProficientSaves { get; set; } = "";
        [JsonPropertyName("proficient_skills")] public string ProficientSkills { get; set; } = "";
        [JsonPropertyName("armor_class")] public int ArmorClass { get; set; } = 10;
        [JsonPropertyName("speed")] public int Speed { get; set; } = 9;
        [JsonPropertyName("hp_max")] public int HpMax { get; set; }
        [JsonPropertyName("hp_current")] public int HpCurrent { get; set; }
        [JsonPropertyName("hp_temp")] public int HpTemp { get; set; }
        [JsonPropertyName("hit_dice")] public string HitDice { get; set; } = "";
        [JsonPropertyName("languages")] public string Languages { get; set; } = "";
        [JsonPropertyName("other_proficiencies")] public string OtherProficiencies { get; set; } = "";
        [JsonPropertyName("coins_pp")] public int CoinsPp { get; set; }
        [JsonPropertyName("coins_po")] public int CoinsPo { get; set; }
        [JsonPropertyName("coins_pe")] public int CoinsPe { get; set; }
        [JsonPropertyName("coins_pa")] public int CoinsPa { get; set; }
        [JsonPropertyName("coins_pr")] public int CoinsPr { get; set; }

        public string OwnerUsername { get; set; } = ""; // popolato lato client, non dal DB
    }
}
