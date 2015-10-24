//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="BackgroundTask.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace Tasks
{
    using AirportInformation;
    using System;
    using System.Diagnostics;
    using Windows.ApplicationModel.Background;
    using Windows.UI.StartScreen;
    using AirportInformation.ViewModels;

    //
    // A background task always implements the IBackgroundTask interface.
    //
    public sealed class BackgroundTask : IBackgroundTask
    {
        /// <summary>
        ///     The Run method is the entry point of a background task.
        /// </summary>
        /// <param name="taskInstance"></param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background " + taskInstance.Task.Name + " running.");
            var def = taskInstance.GetDeferral();
            var tiles = await SecondaryTile.FindAllAsync();
            foreach (var tile in tiles)
            {
                try
                {
                    var metars = await Airport.GetMetarsAsync(tile.TileId);
                    if (metars != null && metars.Length > 0)
                    {
                        await SecondaryTileUpdater.UpdateAsync(tile, metars[0]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            def.Complete();
        }
    }
}
