﻿using System;
using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Opencoding.CommandHandlerSystem;
using UnityEngine;

static class QuickPlayCommandsHandler
{
    private static IGameplayManager _gameplayManager;
    private static IUIManager _uiManager;
    private static IDataManager _dataManager;
    private static IMatchManager _matchManager;

    public static void Initialize()
    {
        CommandHandlers.RegisterCommandHandlers(typeof(QuickPlayCommandsHandler));

        _gameplayManager = GameClient.Get<IGameplayManager>();
        _uiManager = GameClient.Get<IUIManager>();
        _dataManager = GameClient.Get<IDataManager>();
        _matchManager = GameClient.Get<IMatchManager>();
    }

    [CommandHandler(Description = "Print Settings for QuickPlay")]
    private static void Print()
    {
        int playerDeckId = _uiManager.GetPage<GameplayPage>().CurrentDeckId;
        string playerDeckName = _dataManager.CachedDecksData.Decks.First(deck => deck.Id == playerDeckId).Name;

        int opponentDeckId = _gameplayManager.OpponentDeckId;

        string opponentDeckName = _dataManager.CachedAiDecksData.Decks.First(deck => deck.Deck.Id == opponentDeckId).Deck.Name;

        Debug.Log($"(1). Player Horde : {playerDeckName}\n"+
                  $"(2). Enemy AI Horde : {opponentDeckName}\n" +
                  $"(3). Starting Turn : {_gameplayManager.StartingTurn}\n");
    }

    [CommandHandler(Description = "Starts the battle")]
    private static void Start()
    {
        int index = _dataManager.CachedDecksData.Decks.FindIndex(
            deck => deck.Id == _uiManager.GetPage<GameplayPage>().CurrentDeckId);
        if (index == -1)
        {
            int lastPlayerDeckId = _dataManager.CachedUserLocalData.LastSelectedDeckId;
            _uiManager.GetPage<GameplayPage>().CurrentDeckId = lastPlayerDeckId;
        }

        _matchManager.FindMatch(Enumerators.MatchType.LOCAL);
    }

    [CommandHandler(Description = "Set Start Turn  - Player / Enemy")]
    private static void StartingTurn(Enumerators.StartingTurn startingTurn)
    {
        _gameplayManager.StartingTurn = startingTurn;
    }

    [CommandHandler(Description = "Set which player horde to fight with. Accepts deck name.")]
    private static void SetPlayerHorde(string deckName)
    {
        int index = _dataManager.CachedDecksData.Decks.FindIndex(deck => deck.Name == deckName);
        if (index == -1)
        {
            Debug.LogError(deckName + " Not found");
            return;
        }

        _uiManager.GetPage<GameplayPage>().CurrentDeckId = (int)_dataManager.CachedDecksData.Decks[index].Id;
    }

    // TODO : Set Enemy Horde, right now no name exist
    [CommandHandler(Description = "Set which enemy horde to fight with. Accepts deck name.")]
    private static void SetEnemyHorde(string deckName)
    {
        int index = _dataManager.CachedAiDecksData.Decks.FindIndex(aiDeck => aiDeck.Deck.Name == deckName);
        if (index == -1)
        {
            Debug.LogError(deckName + " Not found");
            return;
        }

        GameClient.Get<IGameplayManager>().OpponentDeckId = (int)_dataManager.CachedAiDecksData.Decks[index].Deck.Id;
    }
}
