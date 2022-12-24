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
        const int MatchBlockLeftAdditionalMargin = 50;

        const int BlockMargin = 24;
        const int MatchBlockWidth = 280;
        const int MatchBlockHeight = 210;
        const int FreeBlockHeight = 72;

        const int LogoSize = 60;
        const int TopHeaderMargin = 10;
        const int BottomOffset = 32;
        const int PlayerLineHeight = 40;
        const int PlayerLineWidth = 230;
        const int PlayerLinesOffset = 22;
        const int PlayerLineTextOffset = 24;

        const int MapSize = 80;
        const int MapMargin = 20;
        const int MapTextOffset = 12;

        const int TopHeaderOffset = LogoSize + TopHeaderMargin;
        const int FullBlockHeight = BlockMargin + MatchBlockHeight + BlockMargin;
        const int FullFreeBlockHeight = BlockMargin + FreeBlockHeight + BlockMargin;

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

            var heightsDictionary = new Dictionary<Stage, int>();

            for (int i = 0; i < stages.Length; i++)
            {
                var s = stages[i].Item1;
                heightsDictionary[s] = CalculateStageHeight(stages[i].Item2);
            }

            var maxStageHeight = heightsDictionary.Max(x => x.Value);
            var fullWidth = (MatchBlockWidth + MatchBlockLeftAdditionalMargin) * count;
            var fullHeight = maxStageHeight + TopHeaderOffset + BottomOffset;

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

                var whitePaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 14
                };

                /*var greyPaint = new SKPaint
                {
                    Color = SKColors.White.WithAlpha((byte)(255 * 0.4)),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 14
                };*/

                var mapTextPaint = new SKPaint
                {
                    Color = new SKColor(220, 220, 220),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Center,
                    TextSize = 12
                };

                canvas.DrawImage(SKImage.FromEncodedData(_logo), new SKRect(TopHeaderMargin, TopHeaderMargin, LogoSize, LogoSize), whitePaint);

                for (int i = 0; i < count; i++)
                {
                    var pair = stages[i];

                    var leftOffset = MatchBlockLeftAdditionalMargin + BlockMargin + i * (MatchBlockLeftAdditionalMargin + MatchBlockWidth + BlockMargin + BlockMargin);

                    var blocks = pair.Item2;

                    var center = (info.Height - BottomOffset - TopHeaderMargin - BlockMargin) / 2;

                    var stageHeight = heightsDictionary[pair.Item1];
                    var heightLoss = i > 0 ? heightsDictionary[stages[i - 1].Item1] - stageHeight : 0;

                    var topOffset = TopHeaderOffset + center - stageHeight / 2;

                    for (int k = 0; k < blocks.Length; k++)
                    {
                        var block = blocks[k];
                        var blockTopOffset = topOffset + BlockMargin + (k + 1) * (heightLoss / (blocks.Length+1)) - heightLoss / 2;

                        if (block.IsMatch)
                        {
                            topOffset += MatchBlockHeight + BlockMargin * 2;

                            var match = ((StageBlock.Match)block).Item;

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset + (PlayerLineWidth - MapSize - PlayerLineHeight) / 2,
                                blockTopOffset - 2, 
                                MapSize + 4, 
                                MapSize + 4), 3, 3, backDarkerPaint);

                            var player1Ready = FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player1);
                            var player2Ready = FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player2);

                            if (player1Ready && player2Ready)
                            {
                                canvas.DrawImage(SKImage.FromEncodedData(_maps[match.Map]), SKRect.Create(
                                    leftOffset + (PlayerLineWidth - MapSize - PlayerLineHeight) / 2, 
                                    blockTopOffset,
                                    MapSize,
                                    MapSize), whitePaint);

                                canvas.DrawText(_mapNames[match.Map], new SKPoint(
                                    leftOffset + (PlayerLineWidth - PlayerLineHeight) / 2, 
                                    blockTopOffset + MapSize + MapTextOffset), mapTextPaint);
                            }

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset, 
                                blockTopOffset + MapSize + MapMargin,
                                PlayerLineWidth, 
                                PlayerLineHeight), 5, 5, backPaint);

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset - PlayerLineHeight - 2,
                                blockTopOffset + MapSize + MapMargin - 2,
                                PlayerLineHeight + 4,
                                PlayerLineHeight + 4), 3, 3, backDarkerPaint);

                            if (player1Ready)
                            {
                                var player = match.Player1.Value.Item1;

                                //var paint = IsPlayerLoseOrLeftTheTournament();

                                if (player1Ready && player2Ready)
                                    canvas.DrawImage(SKImage.FromEncodedData(_races[match.Player1.Value.Item2]), SKRect.Create(
                                        leftOffset - PlayerLineHeight - 1,
                                        blockTopOffset + MapSize + MapMargin,
                                        PlayerLineHeight,
                                        PlayerLineHeight), backDarkerPaint);

                                canvas.DrawText(player.Name, new SKPoint(
                                    leftOffset + 8,
                                    blockTopOffset + MapSize + MapMargin + PlayerLineTextOffset), whitePaint);
                            }

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset,
                                blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset,
                                PlayerLineWidth,
                                PlayerLineHeight), 5, 5, backPaint);

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset - PlayerLineHeight - 2,
                                blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset - 2,
                                PlayerLineHeight + 4,
                                PlayerLineHeight + 4), 3, 3, backDarkerPaint);

                            if (player2Ready)
                            {
                                var player = match.Player2.Value.Item1;

                                if (player1Ready && player2Ready)
                                    canvas.DrawImage(SKImage.FromEncodedData(_races[match.Player2.Value.Item2]), SKRect.Create(
                                        leftOffset - PlayerLineHeight - 1,
                                        blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset, 
                                        PlayerLineHeight, 
                                        PlayerLineHeight), backDarkerPaint);

                                canvas.DrawText(player.Name, new SKPoint(
                                    leftOffset + 8, 
                                    blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset + PlayerLineTextOffset), whitePaint);
                            }
                        }
                        else
                        {
                            topOffset += FreeBlockHeight + BlockMargin * 2;


                            var freeBlock = (StageBlock.Free)block;

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset, 
                                blockTopOffset,
                                PlayerLineWidth, 
                                PlayerLineHeight), 5, 5, backPaint);

                            if (FSharpOption<Player>.get_IsSome(freeBlock.Item))
                            {
                                var player = freeBlock.Item.Value;

                                canvas.DrawText(player.Name, new SKPoint(
                                    leftOffset + 8,
                                    blockTopOffset + PlayerLineTextOffset), whitePaint);
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

        private int CalculateStageHeight(StageBlock[] blocks)
        {
            int height = 0;

            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];

                if (block.IsMatch)
                    height += FullBlockHeight;
                else if (block.IsFree)
                    height += FullFreeBlockHeight;
            }

            return height;
        }
    }
}
