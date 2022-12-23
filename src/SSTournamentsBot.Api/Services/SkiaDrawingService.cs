using Microsoft.FSharp.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.Services
{
    public class SkiaDrawingService : IDrawingService
    {
        Dictionary<Map, string> _maps;
        Dictionary<Map, string> _mapNames;
        Dictionary<Race, string> _races;
        string _logo;
        public SkiaDrawingService()
        {
            string PathTo(string fileName)
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName);
            }

            _logo = PathTo("SSTournamentsBot.png");

            _maps = new Dictionary<Map, string>() 
            {
                { Map.BattleMarshes, PathTo("BattleMarshes.jpg") },
                { Map.OuterReaches, PathTo("OuterReaches.jpg") },
                { Map.DeadlyFunArcheology, PathTo("DeadlyFunArcheology.jpg") },
                { Map.FallenCity, PathTo("FallenCity.jpg") },
                { Map.FataMorgana, PathTo("FataMorgana.jpg") },
                { Map.QuestsTriumph,PathTo( "QuestsTriumph.jpg") },
                { Map.ShrineOfExcellion, PathTo("ShrineOfExcellion.jpg") },
                { Map.TitansFall, PathTo("TitanFall.jpg") },
                { Map.TranquilitysEnd, PathTo("TranquilitysEnd.jpg") },
                { Map.MeetingOfMinds, PathTo("MeetingOfMinds.jpg") },
                { Map.SugarOasis, PathTo("sugaroasis.jpg") },
                { Map.BloodRiver, PathTo("bloodriver.jpg") }
            };

            _mapNames = new Dictionary<Map, string>()
            {
                { Map.BattleMarshes, "Battle Marshes" },
                { Map.OuterReaches, "Outer Reaches" },
                { Map.DeadlyFunArcheology, "Deadly Fun Archeology" },
                { Map.FallenCity,"Fallen City" },
                { Map.FataMorgana, "Fata Morgana" },
                { Map.QuestsTriumph, "Quest's Triumph" },
                { Map.ShrineOfExcellion, "Shrine Of Excellion" },
                { Map.TitansFall, "Titan Fall" },
                { Map.TranquilitysEnd, "Tranquility's End" },
                { Map.MeetingOfMinds, "Meeting Of Minds" },
                { Map.SugarOasis, "Sugar Oasis" },
                { Map.BloodRiver, "Blood River" }
            };

            _races = new Dictionary<Race, string>()
            {
                { Race.Chaos, PathTo("chaos.png") },
                { Race.DarkEldar, PathTo("darkEldar.png") },
                { Race.Eldar, PathTo("eldar.png") },
                { Race.ImperialGuard, PathTo("ig.png") },
                { Race.Necrons, PathTo("necron.png") },
                { Race.Orks, PathTo("ork.png") },
                { Race.SisterOfBattle, PathTo("sob.png") },
                { Race.SpaceMarines, PathTo("spaceMarine.png") },
                { Race.Tau, PathTo("tau.png") }
            };
        }

        public byte[] DrawToImage((Stage, StageBlock[])[] stages)
        {
            var count = stages.Length;

            var matchBlockLeftAddidionalMargin = 50;

            var matchBlockMargin = 24;
            var matchBlockWidth = 230;
            var matchBlockHeight = 200;

            var logoSize = 60;
            var topHeaderMargin = 10;
            var topHeaderOffset = logoSize + matchBlockMargin + topHeaderMargin;
            var bottomOffset = 10;

            var fullBlockHeight = matchBlockMargin + matchBlockHeight + matchBlockMargin;
            
            var mapSize = 80;
            var playerLineHeight = 40;
            var playerLinesOffset = 22;
            var playerLineTextOffset = 22;
            var mapTextOffset = 12;
            var mapMargin = 20;

            var biggestStage = stages.OrderByDescending(x => x.Item2.Length).FirstOrDefault();
            var maxMatches = biggestStage.Item2.Length;


            var fullWidth = matchBlockLeftAddidionalMargin + (matchBlockWidth + matchBlockLeftAddidionalMargin) * count + matchBlockMargin;
            var fullHeight = fullBlockHeight * maxMatches + topHeaderOffset;

            var info = new SKImageInfo(fullWidth, fullHeight);
            
            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;

                canvas.Clear(new SKColor(54, 57, 63));

                var backPaint = new SKPaint
                {
                    Color = new SKColor(47, 49, 54),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 16
                };

                var backDarkerPaint = new SKPaint
                {
                    Color = new SKColor(40, 40, 50),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 16
                };

                var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 14
                };

                var mapTextPaint = new SKPaint
                {
                    Color = new SKColor(220, 220, 220),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Center,
                    TextSize = 12
                };

                canvas.DrawImage(SKImage.FromEncodedData(_logo), new SKRect(topHeaderMargin, topHeaderMargin, logoSize, logoSize), textPaint);

                for (int i = 0; i < count; i++)
                {
                    var pair = stages[i];

                    var leftOffset = matchBlockLeftAddidionalMargin + matchBlockMargin + i * (matchBlockLeftAddidionalMargin + matchBlockWidth + matchBlockMargin + matchBlockMargin);

                    var matcheOrFreeBlocks = pair.Item2;

                    var center = (info.Height - bottomOffset - topHeaderMargin - matchBlockMargin) / 2;

                    var topOffset = topHeaderOffset + center - matcheOrFreeBlocks.Length * fullBlockHeight / 2;

                    for (int k = 0; k < matcheOrFreeBlocks.Length; k++)
                    {
                        var matchOrFree = matcheOrFreeBlocks[k];
                        var blockTopOffset = topOffset + k * (matchBlockHeight + matchBlockMargin * 2) + mapSize + mapMargin;

                        if (matchOrFree.IsMatch)
                        {
                            var match = ((StageBlock.Match)matchOrFree).Item;

                            canvas.DrawRoundRect(SKRect.Create(leftOffset + (matchBlockWidth - mapSize - playerLineHeight) / 2, blockTopOffset - mapSize - mapMargin - 2, mapSize + 4, mapSize + 4), 3, 3, backDarkerPaint);

                            var player1Ready = FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player1);
                            var player2Ready = FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player2);

                            if (player1Ready && player2Ready)
                            {
                                canvas.DrawImage(SKImage.FromEncodedData(_maps[match.Map]), SKRect.Create(leftOffset + (matchBlockWidth - mapSize - playerLineHeight) / 2, blockTopOffset - mapSize - mapMargin, mapSize, mapSize), textPaint);
                                canvas.DrawText(_mapNames[match.Map], new SKPoint(leftOffset + (matchBlockWidth - playerLineHeight) / 2, blockTopOffset - mapMargin + mapTextOffset), mapTextPaint);
                            }

                            canvas.DrawRoundRect(SKRect.Create(leftOffset, blockTopOffset, matchBlockWidth, playerLineHeight), 5, 5, backPaint);
                            canvas.DrawRoundRect(SKRect.Create(leftOffset - playerLineHeight - 2, blockTopOffset - 2, playerLineHeight + 4, playerLineHeight + 4), 3, 3, backDarkerPaint);

                            if (player1Ready)
                            {
                                var player = match.Player1.Value.Item1;

                                if (player1Ready && player2Ready)
                                    canvas.DrawImage(SKImage.FromEncodedData(_races[match.Player1.Value.Item2]), SKRect.Create(leftOffset - playerLineHeight - 1, blockTopOffset, playerLineHeight, playerLineHeight), backDarkerPaint);
                                canvas.DrawText(player.Name, new SKPoint(leftOffset + 8, blockTopOffset + playerLineTextOffset), textPaint);
                            }

                            canvas.DrawRoundRect(SKRect.Create(leftOffset, blockTopOffset + playerLineHeight + playerLinesOffset, matchBlockWidth, playerLineHeight), 5, 5, backPaint);
                            canvas.DrawRoundRect(SKRect.Create(leftOffset - playerLineHeight - 2, blockTopOffset + playerLineHeight + playerLinesOffset - 2, playerLineHeight + 4, playerLineHeight + 4), 3, 3, backDarkerPaint);

                            if (player2Ready)
                            {
                                var player = match.Player2.Value.Item1;

                                if (player1Ready && player2Ready)
                                    canvas.DrawImage(SKImage.FromEncodedData(_races[match.Player2.Value.Item2]), SKRect.Create(leftOffset - playerLineHeight - 1, blockTopOffset + playerLineHeight + playerLinesOffset, playerLineHeight, playerLineHeight), backDarkerPaint);
                                canvas.DrawText(player.Name, new SKPoint(leftOffset + 8, blockTopOffset + playerLineHeight + playerLinesOffset + playerLineTextOffset), textPaint);
                            }
                        }
                        else
                        {
                            var freeBlock = (StageBlock.Free)matchOrFree;

                            canvas.DrawRoundRect(SKRect.Create(leftOffset, blockTopOffset, matchBlockWidth, playerLineHeight), 5, 5, backPaint);

                            if (FSharpOption<Player>.get_IsSome(freeBlock.Item))
                            {
                                var player = freeBlock.Item.Value;
                                canvas.DrawText(player.Name, new SKPoint(leftOffset + 8, blockTopOffset + playerLineTextOffset), textPaint);
                            }
                        }
                    }
                }

                using (var image = surface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = new MemoryStream())
                {
                    data.SaveTo(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
