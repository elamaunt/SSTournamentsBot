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
        const int PlayerLineLeftTextOffset = 12;

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
        string _dead;
        string _techLose;
        string _winner;
        string _font;
        public SkiaDrawingService()
        {
            string PathTo(string fileName)
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName);
            }

            _logo = PathTo("SSTournamentsBot.png");
            _dead = PathTo("Blood.png");
            _techLose = PathTo("TechLose.png");
            _winner = PathTo("Winner.png");
            _font = PathTo("roboto-medium.ttf");

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

        public byte[] DrawToImage(Tournament tournament, (Stage, StageBlock[])[] stages)
        {
            var count = stages.Length;
            var lastBlocks = stages.Last().Item2;

            Player tournamentWinner = null;

            if (lastBlocks.Length == 1)
            {
                var lastBlock = lastBlocks[0];

                if (lastBlock.IsFree)
                    tournamentWinner = ((StageBlock.Free)lastBlock).Item.ValueOrDefault();
            }

            var heightsDictionary = new Dictionary<Stage, int>();

            for (int i = 0; i < stages.Length; i++)
            {
                var s = stages[i].Item1;
                heightsDictionary[s] = CalculateStageHeight(stages[i].Item2);
            }

            var maxStageHeight = heightsDictionary.Max(x => x.Value);
            var fullWidth = (MatchBlockWidth + MatchBlockLeftAdditionalMargin + BlockMargin) * count + BlockMargin * (count - 1);
            var fullHeight = maxStageHeight + TopHeaderOffset + BottomOffset;

            var info = new SKImageInfo(fullWidth, fullHeight);
            
            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;

                canvas.Clear(new SKColor(54, 57, 63));

                var typeface = SKTypeface.FromFile(_font);

                var backPaint = new SKPaint
                {
                    Color = new SKColor(47, 49, 54),
                    IsAntialias = true
                };

                var backDarkerPaint = new SKPaint
                {
                    Color = new SKColor(40, 40, 50),
                    IsAntialias = true
                };

                var tournamentWinnerTextPaint = new SKPaint
                {
                    Color = SKColors.Yellow,
                    IsAntialias = true,
                    Style = SKPaintStyle.StrokeAndFill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 15,
                    Typeface = typeface
                };

                var titlePaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 28,
                    Typeface = typeface
                };

                var subTitlePaint = new SKPaint
                {
                    Color = new SKColor(30, 30, 40),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 13,
                    Typeface = typeface
                };

                var identityIconPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 15,
                    Typeface = typeface,
                   // BlendMode = SKBlendMode.Multiply
                };

                var whitePaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 15,
                    Typeface = typeface
                };

                var notCompletedTextPaint = new SKPaint
                {
                    Color = new SKColor(210, 210, 210),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 15,
                    Typeface = typeface
                };

                var notActiveTextPaint = new SKPaint
                {
                    Color = new SKColor(223, 16, 24),
                    IsAntialias = true,
                    Style = SKPaintStyle.StrokeAndFill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 15,
                    Typeface = typeface
                };

                var mapTextPaint = new SKPaint
                {
                    Color = new SKColor(220, 220, 220),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Center,
                    TextSize = 13,
                    Typeface = typeface
                };

                canvas.DrawImage(SKImage.FromEncodedData(_logo), SKRect.Create(TopHeaderMargin, TopHeaderMargin, LogoSize, LogoSize), whitePaint);
                canvas.DrawText($"REGULAR TOURNAMENT  {tournament.Id}  |  {tournament.StartDate.Value.PrettyShortDatePrint()}", new SKPoint(TopHeaderMargin + LogoSize + TopHeaderMargin, TopHeaderMargin + LogoSize / 2), titlePaint);
                canvas.DrawText($"SS Tournaments Bot | powered by elamaunt", new SKPoint(TopHeaderMargin + LogoSize + TopHeaderMargin, TopHeaderMargin + LogoSize / 2 + 18), subTitlePaint);
                
                var blockPoints = new Dictionary<(int StageIndex, int TargetSlotIndex), (Player Player, SKPoint Point, bool Free)[]>();

                for (int i = 0; i < count; i++)
                {
                    var pair = stages[i];

                    var leftOffset = MatchBlockLeftAdditionalMargin + BlockMargin + i * (MatchBlockLeftAdditionalMargin + MatchBlockWidth + BlockMargin + BlockMargin);

                    var blocks = pair.Item2;

                    var center = (info.Height - BottomOffset - TopHeaderMargin - BlockMargin) / 2;

                    var stageHeight = heightsDictionary[pair.Item1];
                    var heightLoss = i > 0 ? heightsDictionary[stages[i - 1].Item1] - stageHeight : 0;

                    var topOffset = TopHeaderOffset + center - stageHeight / 2;

                    int currentSlotsCounter = 0;
                    for (int k = 0; k < blocks.Length; k++)
                    {
                        var block = blocks[k];
                        var blockTopOffset = topOffset + BlockMargin + (k + 1) * (heightLoss / (blocks.Length+1)) - heightLoss / 2;

                        if (block.IsMatch)
                        {
                            var match = ((StageBlock.Match)block).Item;

                            // Saving points

                            blockPoints.Add((i, k), new (Player Player, SKPoint Point, bool Free)[] {
                                (match.Player1.ValueOrDefault()?.Item1, new SKPoint(leftOffset + PlayerLineWidth, blockTopOffset + MapSize + MapMargin + PlayerLineHeight / 2), false),
                                (match.Player2.ValueOrDefault()?.Item1 , new SKPoint(leftOffset + PlayerLineWidth, blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset + PlayerLineHeight / 2), false)
                            });

                            topOffset += MatchBlockHeight + BlockMargin * 2;


                            var player1Ready = FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player1);
                            var player2Ready = FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player2);

                            // Draw the line between stages
                            if (i > 0)
                            {
                                var points1 = blockPoints[(i - 1, currentSlotsCounter)];

                                for (int h = 0; h < points1.Length; h++)
                                {
                                    var p = points1[h];
                                    var player = match.Player1.ValueOrDefault()?.Item1;
                                    var active = player == null ? (bool?)null : player == p.Player;
                                    DrawLineTo(canvas, p.Free ? 0.7f : 0.3f, p.Point, new SKPoint(leftOffset - PlayerLineHeight - 2, blockTopOffset + MapSize + MapMargin + PlayerLineHeight / 2), active);
                                }

                                var points2 = blockPoints[(i - 1, currentSlotsCounter + 1)];

                                for (int h = 0; h < points2.Length; h++)
                                {
                                    var p = points2[h];
                                    var player = match.Player2.ValueOrDefault()?.Item1;
                                    var active = player == null ? (bool?)null : player == p.Player;
                                    DrawLineTo(canvas, p.Free ? 0.7f : 0.3f, p.Point, new SKPoint(leftOffset - PlayerLineHeight - 2, blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset + PlayerLineHeight / 2), active);
                                }
                            }

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset + (PlayerLineWidth - MapSize - PlayerLineHeight) / 2 - 2,
                                blockTopOffset - 2,
                                MapSize + 4,
                                MapSize + 4), 3, 3, backDarkerPaint);

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

                            // Draw the block
                           
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

                                if (player1Ready && player2Ready)
                                {
                                    canvas.DrawImage(SKImage.FromEncodedData(_races[match.Player1.Value.Item2]), SKRect.Create(
                                        leftOffset - PlayerLineHeight - 1,
                                        blockTopOffset + MapSize + MapMargin,
                                        PlayerLineHeight,
                                        PlayerLineHeight), backDarkerPaint);
                                }

                                if (match.Result.IsNotCompleted)
                                {
                                    canvas.DrawText(player.Name, new SKPoint(
                                        leftOffset + PlayerLineLeftTextOffset,
                                        blockTopOffset + MapSize + MapMargin + PlayerLineTextOffset), notCompletedTextPaint);
                                }
                                else
                                {
                                    Player winner = null;
                                    bool techWin = false;

                                    if (match.Result.IsWinner)
                                        winner = ((MatchResult.Winner)match.Result).Item1;
                                    else if (match.Result.IsTechnicalWinner)
                                    {
                                        winner = ((MatchResult.TechnicalWinner)match.Result).Item1;
                                        techWin = true;
                                    }

                                    if (winner != null)
                                        if (winner == player)
                                        {
                                            canvas.DrawText(player.Name, new SKPoint(
                                                leftOffset + PlayerLineLeftTextOffset,
                                                blockTopOffset + MapSize + MapMargin + PlayerLineTextOffset), tournamentWinner == player ? tournamentWinnerTextPaint : whitePaint);
                                        }
                                        else
                                        {
                                            canvas.DrawText(player.Name, new SKPoint(
                                                leftOffset + PlayerLineLeftTextOffset,
                                                blockTopOffset + MapSize + MapMargin + PlayerLineTextOffset), notActiveTextPaint);

                                            canvas.DrawImage(SKImage.FromEncodedData(techWin ? _techLose : _dead), new SKPoint(leftOffset + PlayerLineWidth - 100, blockTopOffset + MapSize + MapMargin));
                                        }
                                }

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
                                {
                                    canvas.DrawImage(SKImage.FromEncodedData(_races[match.Player2.Value.Item2]), SKRect.Create(
                                        leftOffset - PlayerLineHeight - 1,
                                        blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset,
                                        PlayerLineHeight,
                                        PlayerLineHeight), backDarkerPaint);
                                }

                                if (match.Result.IsNotCompleted)
                                {
                                    canvas.DrawText(player.Name, new SKPoint(
                                        leftOffset + PlayerLineLeftTextOffset,
                                        blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset + PlayerLineTextOffset), notCompletedTextPaint);
                                }
                                else
                                {
                                    Player winner = null;
                                    bool techWin = false;

                                    if (match.Result.IsWinner)
                                        winner = ((MatchResult.Winner)match.Result).Item1;
                                    else if (match.Result.IsTechnicalWinner)
                                    {
                                        winner = ((MatchResult.TechnicalWinner)match.Result).Item1;
                                        techWin = true;
                                    }

                                    if (winner != null)
                                        if (winner == player)
                                        {
                                            canvas.DrawText(player.Name, new SKPoint(
                                                leftOffset + PlayerLineLeftTextOffset,
                                                blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset + PlayerLineTextOffset), tournamentWinner == player ? tournamentWinnerTextPaint : whitePaint);
                                        }
                                        else
                                        {
                                            canvas.DrawText(player.Name, new SKPoint(
                                                leftOffset + PlayerLineLeftTextOffset,
                                                blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset + PlayerLineTextOffset), notActiveTextPaint);

                                            canvas.DrawImage(SKImage.FromEncodedData(techWin ? _techLose : _dead), new SKPoint(leftOffset + PlayerLineWidth - 100, blockTopOffset + MapSize + MapMargin + PlayerLineHeight + PlayerLinesOffset));
                                        }
                                }
                            }

                            currentSlotsCounter += 2;
                        }
                        else
                        {
                            topOffset += FreeBlockHeight + BlockMargin * 2;


                            var freeBlock = (StageBlock.Free)block;

                            if (i > 0)
                            {
                                var points = blockPoints[(i - 1, currentSlotsCounter)];

                                for (int h = 0; h < points.Length; h++)
                                {
                                    var p = points[h];
                                    var player = freeBlock.Item.ValueOrDefault();
                                    var active = player == null ? (bool?)null : player == p.Player;
                                    DrawLineTo(canvas, p.Free ? 0.7f : 0.3f, p.Point, new SKPoint(leftOffset, blockTopOffset + PlayerLineHeight / 2), active);
                                }
                            }

                            blockPoints.Add((i, k), new (Player Player, SKPoint Point, bool Free)[] {
                                (freeBlock.Item.ValueOrDefault(), new SKPoint(leftOffset + PlayerLineWidth, blockTopOffset + PlayerLineHeight / 2), true)
                            });

                            canvas.DrawRoundRect(SKRect.Create(
                                leftOffset, 
                                blockTopOffset,
                                PlayerLineWidth, 
                                PlayerLineHeight), 5, 5, backPaint);

                            if (FSharpOption<Player>.get_IsSome(freeBlock.Item))
                            {
                                var player = freeBlock.Item.Value;

                                canvas.DrawText(player.Name, new SKPoint(
                                    leftOffset + PlayerLineLeftTextOffset,
                                    blockTopOffset + PlayerLineTextOffset), tournamentWinner == player ? tournamentWinnerTextPaint : whitePaint);

                                //if (blocks.Length == 1)
                                //    canvas.DrawImage(SKImage.FromEncodedData(_winner), new SKPoint(leftOffset + PlayerLineWidth - 100, blockTopOffset));
                            }

                            currentSlotsCounter++;
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

        private void DrawLineTo(SKCanvas canvas, float mult, SKPoint from, SKPoint to, bool? active)
        {
            var paint = new SKPaint
            {
                Color = active.HasValue ? (active.Value ? new SKColor(167, 169, 160) : SKColor.Empty) : new SKColor(24, 24, 24),
                IsAntialias = true
            };

            var middleX = from.X + (to.X - from.X) * mult;

            var p0 = new SKPoint(middleX, from.Y);
            var p1 = new SKPoint(middleX, to.Y);

            canvas.DrawLine(from, p0, paint);
            canvas.DrawLine(p0, p1, paint);
            canvas.DrawLine(p1, to, paint);
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
