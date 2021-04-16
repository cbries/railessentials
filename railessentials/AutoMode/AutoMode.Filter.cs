// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoMode.Filter.cs

using System;
using ecoslib.Entities;
using railessentials.Analyzer;
using railessentials.Route;

namespace railessentials.AutoMode
{
    public partial class AutoMode
    {
        /// <summary>
        /// Filter routes by target block enabled stated,
        /// only use routes where target blocks are enabled.
        /// </summary>
        /// <param name="routeList"></param>
        /// <param name="sideToLeave"></param>
        /// <param name="locData"></param>
        /// <returns></returns>
        public RouteList FilterByBlockEnabled(
            RouteList routeList,
            SideMarker sideToLeave,
            Locomotives.Data locData)
        {
            var res = new RouteList();
            foreach (var it in routeList)
            {
                var targetBlock = it.Blocks[1];
                var targetBlockIdentifier = targetBlock.identifier;
                if (string.IsNullOrEmpty(targetBlockIdentifier)) continue;

                var targetFbData = GetFeedbackDataOf(targetBlockIdentifier, sideToLeave);
                if (targetFbData == null) continue;

                //
                // when the target block is disabled, do not use for routing
                //
                if (targetFbData.Settings != null && targetFbData.Settings.ContainsKey("BlockEnabled"))
                {
                    var blockEnabled = targetFbData.Settings["BlockEnabled"];
                    if (!blockEnabled) 
                        continue;
                }

                res.Add(it);
            }
            return res;
        }

        /// <summary>
        /// Filter routes by options, does not check "BlockEnabled" option.
        /// See FilterByBlockEnabled()
        /// </summary>
        /// <param name="routeList"></param>
        /// <param name="sideToLeave"></param>
        /// <param name="locData"></param>
        /// <returns></returns>
        public RouteList FilterByAllowedOptions(
            RouteList routeList,
            SideMarker sideToLeave,
            Locomotives.Data locData)
        {
            var res = new RouteList();
            foreach (var it in routeList)
            {
                var targetBlock = it.Blocks[1];
                var targetBlockIdentifier = targetBlock.identifier;
                if (string.IsNullOrEmpty(targetBlockIdentifier)) continue;

                var targetFbData = GetFeedbackDataOf(targetBlockIdentifier, sideToLeave);
                if (targetFbData == null) continue;
                
                //
                // the most interesting part of this method
                //
                if (IsLocAllowedForTargetBlock(locData, targetFbData))
                    res.Add(it);
            }
            return res;
        }

        /// <summary>
        /// Filter all routes with blocks which are blocked by any other routes.
        /// https://github.com/cbries/railessentials/wiki/Lock-Additional-Blocks-during-Locomotive-Traveling
        /// </summary>
        /// <param name="routeList"></param>
        /// <param name="sideToLeave"></param>
        /// <returns></returns>
        public RouteList FilterByBlockedRoutes(
            RouteList routeList,
            SideMarker sideToLeave)
        {
            var res = new RouteList();
            foreach (var it in routeList)
            {
                var targetBlock = it.Blocks[1];
                var targetBlockIdentifier = targetBlock.identifier;
                if (string.IsNullOrEmpty(targetBlockIdentifier)) continue;

                var targetFbData = GetFeedbackDataOf(targetBlockIdentifier, sideToLeave);
                if (targetFbData == null) continue;

                var lockedBy = targetFbData.LockedByBlock;
                if (string.IsNullOrEmpty(lockedBy))
                {
                    res.Add(it);
                }
                else
                {
                    var fromBlock = it.Blocks[0];
                    var fromBlockIdentifier = fromBlock.identifier;

                    if (lockedBy.StartsWith(fromBlockIdentifier, StringComparison.OrdinalIgnoreCase))
                        res.Add(it);
                }
            }
            return res;
        }

        /// <summary>
        /// in case there is no route to leave on the sideToLeave
        /// probably the trains' direction must change, if change
        /// is allowed:
        /// (1) check for a new route on the opposide sideToLeave
        /// (2) if one or more route available, check if the train is allowed to change the direction (as well the block) [no check if cleaningPriority:=true]
        /// (3) change the direction
        /// (4) change the sideToLeave
        /// (5) ...start the additional route selection routines
        /// </summary>
        /// <param name="routeList"></param>
        /// <param name="occBlock"></param>
        /// <param name="sideToLeave"></param>
        /// <param name="originalSideEntered"></param>
        /// <param name="locDataEcos"></param>
        /// <param name="locData"></param>
        /// <param name="routesOnOpposide"></param>
        /// <param name="cleaningPriority"></param>
        /// <returns></returns>
        public bool CheckOpposide(
            RouteList routeList,
            Occ.OccBlock occBlock,
            SideMarker sideToLeave,
            SideMarker originalSideEntered,
            Locomotive locDataEcos,
            Locomotives.Data locData,
            out RouteList routesOnOpposide,
            bool cleaningPriority = false)
        {
            routesOnOpposide = new RouteList();

            string step4enterBlockSide;

            var occFromBlock = occBlock.FromBlock;
            if (string.IsNullOrEmpty(occFromBlock)) return false;

            LogInfo($"The side to leave {sideToLeave} does not have any route to take.");
            if (sideToLeave == SideMarker.Minus)
            {
                step4enterBlockSide = "'-' Side";
                sideToLeave = SideMarker.Plus;
            }
            else
            {
                step4enterBlockSide = "'+' Side";
                sideToLeave = SideMarker.Minus;
            }

            #region (1)

            //
            // (1)
            //
            routesOnOpposide = routeList.GetRoutesWithFromBlock(occFromBlock, sideToLeave, true);
            if (routesOnOpposide.Count == 0)
            {
                LogInfo($"The other side to leave {sideToLeave} does not have any route to take.");
                LogInfo($"No route to take from {occFromBlock} for Locomotive({locDataEcos.Name ?? "-"}).");
                return false;
            }

            #endregion (1)

            #region (2)

            //
            // (2)
            //
            if (cleaningPriority == false)
            {
                if (locData.Settings.ContainsKey("OptionDirection"))
                {
                    var locState = locData.Settings["OptionDirection"];
                    if (!locState)
                    {
                        LogInfo($"Locomotive({locDataEcos.Name}) is not allowed to change the direction.");
                        return false;
                    }
                }

                var fbData = GetFeedbackDataOf(occBlock.FromBlock, originalSideEntered);
                if (fbData == null)
                {
                    LogInfo($"No feedback data available for block {occBlock.FromBlock}.");
                    return false;
                }

                if (fbData.Settings.ContainsKey("OptionDirection"))
                {
                    var blockState = fbData.Settings["OptionDirection"];
                    if (!blockState)
                    {
                        LogInfo($"Block({fbData.BlockId}) does not allow to change the direction.");
                        return false;
                    }
                }
            }

            #endregion (2)

            #region (3)

            //
            // (3)
            //
            var currentDirection = locDataEcos.Direction;
            var newDirection = currentDirection == 1 ? 0 : 1;
            if (_ctx.IsSimulationMode())
            {
                locDataEcos.ChangeDirectionSimulation(newDirection == 1);
                _ctx.SaveAll();
                _ctx?._sniffer?.TriggerDataProviderModifiedForSimulation();
            }
            else
            {
                locDataEcos.ChangeDirection(newDirection == 1);
                _ctx?._sniffer?.SendCommandsToEcosStation();
            }

            #endregion (3)

            #region (4)

            //
            // (4)
            //
            // EnterBlockSide = "'+' Side"
            // EnterBlockSide = "'-' Side"
            if (string.IsNullOrEmpty(step4enterBlockSide))
            {
                LogInfo($"Invalid enterBlockSide value for Locomotive({locDataEcos.Name}).");
                return false;
            }

            locData.EnterBlockSide = step4enterBlockSide;
            SaveLocomotivesAndPromote();
            SaveOccAndPromote();

            #endregion (4)

            return routesOnOpposide.Count > 0;
        }
    }
}
