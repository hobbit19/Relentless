﻿
using System;
using System.Threading.Tasks;
using Loom.Google.Protobuf.Collections;
using Loom.Unity3d.Zb;
using UnityEngine;
using Deck = LoomNetwork.CZB.Data.Deck;
using ZbDeck = Loom.Unity3d.Zb.Deck;

public partial class LoomManager
{
    private const string GetDeckDataMethod = "ListDecks";
    private const string DeleteDeckMethod = "DeleteDeck";
    private const string AddDeckMethod = "CreateDeck";
    private const string EditDeckMethod = "EditDeck";
    
    public async Task<ListDecksResponse> GetDecks(string userId)
    {
        var request = new ListDecksRequest {
            UserId = userId
        };
        
        return await Contract.StaticCallAsync<ListDecksResponse>(GetDeckDataMethod, request);
    }

    public async Task DeleteDeck(string userId, string deckId, Action<string> errorResult)
    {
        var request = new DeleteDeckRequest {
            UserId = userId,
            DeckName = deckId
        };
        
        try
        {
            await Contract.CallAsync(DeleteDeckMethod, request);
            errorResult?.Invoke(string.Empty);
        }
        catch (Exception ex)
        {
            //Debug.Log("Exception = " + ex);
            errorResult?.Invoke(ex.ToString());
        }
    }

    public async Task EditDeck(string userId, Deck deck, Action<string> errorResult)
    {
        var cards = new RepeatedField<CardCollection>();
            
        for (var i = 0; i < deck.cards.Count; i++)
        {
            var cardInCollection = new CardCollection
            {
                CardName = deck.cards[i].cardName,
                Amount = deck.cards[i].amount
            };
            Debug.Log("Card in collection = " + cardInCollection.CardName + " , " + cardInCollection.Amount);
            cards.Add(cardInCollection);
        }
        
        var request = new EditDeckRequest
        {
            UserId = userId,
            Deck = new ZbDeck
            {
                Name = deck.name,
                HeroId = deck.heroId,
                Cards = {cards}
            }
        };
        
        try
        {
            await Contract.CallAsync(EditDeckMethod, request);
            errorResult?.Invoke(string.Empty);
        }
        catch (Exception ex)
        {
            //Debug.Log("Exception = " + ex);
            errorResult?.Invoke(ex.ToString());
        }
    }

    public async Task AddDeck(string userId, Deck deck, Action<string> errorResult)
    {
        var cards = new RepeatedField<CardCollection>();
            
        for (var i = 0; i < deck.cards.Count; i++)
        {
            var cardInCollection = new CardCollection
            {
                CardName = deck.cards[i].cardName,
                Amount = deck.cards[i].amount
            };
            Debug.Log("Card in collection = " + cardInCollection.CardName + " , " + cardInCollection.Amount);
            cards.Add(cardInCollection);
        }
            
        var request = new CreateDeckRequest
        {
            UserId = userId,
            Deck = new ZbDeck
            {
                Name = deck.name,
                HeroId = deck.heroId,
                Cards = {cards}
            }
        };

        try
        {
            await Contract.CallAsync(AddDeckMethod, request);
            errorResult?.Invoke(string.Empty);
        }
        catch (Exception ex)
        {
            //Debug.Log("Exception = " + ex);
            errorResult?.Invoke(ex.ToString());
        }
        
    }
    
    
}
