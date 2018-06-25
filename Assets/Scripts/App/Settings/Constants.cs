﻿using System.Collections.Generic;

namespace GrandDevs.CZB.Common
{
    public class Constants
    {
        public const string SPACE = " ";

        internal const string CURRENT_VERSION = "0.0.13.2.ver";

        internal const string LOCAL_USER_DATA_FILE_PATH = "0412DD0.dat";
		internal const string LOCAL_CARDS_LIBRARY_DATA_FILE_PATH = "card_library_data.json";
		internal const string LOCAL_COLLECTION_DATA_FILE_PATH = "collection_data.json";
		internal const string LOCAL_HEROES_DATA_FILE_PATH = "heroes_data.json";
		internal const string LOCAL_DECKS_DATA_FILE_PATH = "decks_data.json";
        internal const string LOCAL_OPPONENT_DECKS_DATA_FILE_PATH = "opponent_decks_data.json";
        internal const string LOCAL_OPPONENT_ACTIONS_LIBRARY_DATA_FILE_PATH = "action_library_data.json";
        internal const string LOCAL_CREDITS_DATA_FILE_PATH = "credits_data.json";

        internal const string PRIVATE_ENCRYPTION_KEY_FOR_APP = "sagatdsgsg7687sdg587a8gs89";

        internal static bool DATA_ENCRYPTION_ENABLED = false;
            
        internal static bool DEBUG_MODE = false;

		internal static uint HEROES_AMOUNT = 9;
		internal static uint DECK_MAX_SIZE = 30;
		internal static uint CARD_COMMON_MAX_COPIES = 4;
		internal static uint CARD_RARE_MAX_COPIES = 2;
        internal static uint CARD_EPIC_MAX_COPIES = 2;
        internal static uint CARD_LEGENDARY_MAX_COPIES = 1;
		internal static uint CARDS_IN_PACK = 5;

        internal static uint MAX_BOARD_CREATURES = 6;

        internal static int DEFAULT_TURN_DURATION = 30;

        internal const string TAG_PLAYER_OWNED = "PlayerOwned";
        internal const string TAG_OPPONENT_OWNED = "OpponentOwned";


        internal const string ZONE_HAND = "Hand";
        internal const string ZONE_BOARD = "Board";
        internal const string ZONE_DECK = "Deck";
        internal const string ZONE_GRAVEYARD = "Graveyard";

        internal const string TAG_LIFE = "Life";
        internal const string TAG_DAMAGE = "Damage";
        internal const string TAG_HP = "HP";
        internal const string TAG_MANA = "Mana";

        internal const string STAT_DAMAGE = "DMG";
        internal const string STAT_HP = "HP";

        internal const string CONTENT_FOLDER_NAME = "SpreadsheetsData/";
        internal const string SPREADSHEET_FILE_FORMAT = ".csv";

        internal const string CARD_SOUND_PLAY = "P";
        internal const string CARD_SOUND_ATTACK = "A";
        internal const string CARD_SOUND_DEATH = "D";

        internal static UnityEngine.Vector3 VFX_OFFSET = UnityEngine.Vector3.zero;

        // SOUNDS VOLUME'S
        internal static float ZOMBIES_SOUND_VOLUME = 0.05f;
        internal static float TUTORIAL_SOUND_VOLUME = 1f;
        internal static float ARRIVAL_SOUND_VOLUME = 0.1f;
        internal static float CREATURE_ATTACK_SOUND_VOLUME = 0.05f;
        internal static float END_TURN_CLICK_SOUND_VOLUME = 0.1f;
        internal static float HERO_DEATH_SOUND_VOLUME = 0.1f;
        internal static float CARDS_MOVE_SOUND_VOLUME = 0.025f;
        internal static float BACKGROUND_SOUND_VOLUME = 0.05f;
        internal static float SFX_SOUND_VOLUME = 0.15f;


        internal static float DELAY_TO_PLAY_DEATH_SOUND_OF_CREATURE = 2f;

        internal const bool DEV_MODE = false;
    }
}