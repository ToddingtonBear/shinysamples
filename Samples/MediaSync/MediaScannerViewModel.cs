﻿using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.MediaSync;
using Shiny.MediaSync.Infrastructure;


namespace Samples.MediaSync
{
    public class MediaScannerViewModel : ViewModel
    {
        public MediaScannerViewModel(IDialogs dialogs, IMediaGalleryScanner? scanner = null)
        {
            this.RunQuery = ReactiveCommand.CreateFromTask(async () =>
            {
                if (scanner == null)
                {
                    await dialogs.Alert("Media scanner not supported");
                    return;
                }
                var result = await scanner.RequestAccess();
                if (result != AccessState.Available)
                {
                    await dialogs.Alert("Invalid Status - " + result);
                    return;
                }
                var mediaTypes = MediaTypes.None;
                if (this.IncludeAudio)
                    mediaTypes |= MediaTypes.Audio;
                if (this.IncludeImages)
                    mediaTypes |= MediaTypes.Image;
                if (this.IncludeVideos)
                    mediaTypes |= MediaTypes.Video;

                var list = await scanner.Query(mediaTypes, this.SyncFrom);
                this.List.ReplaceAll(list.Select(x => new CommandItem
                { 
                    Text = $"{x.Type} - {x.FilePath}",
                    ImageUri = x.Type == MediaTypes.Audio ? null : x.FilePath
                }));
            });
            this.BindBusyCommand(this.RunQuery);
        }


        public ICommand RunQuery { get; }
        [Reactive] public bool IncludeVideos { get; set; } = true;
        [Reactive] public bool IncludeImages { get; set; } = true;
        [Reactive] public bool IncludeAudio { get; set; } = true;
        [Reactive] public DateTime SyncFrom { get; set; } = DateTime.Now.AddDays(-30);
        public ObservableList<CommandItem> List { get; } = new ObservableList<CommandItem>();


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.WhenAnyValue(
                    x => x.IncludeVideos,
                    x => x.IncludeImages,
                    x => x.IncludeAudio,
                    x => x.SyncFrom
                )
                .Skip(1)
                .Subscribe(_ => this.RunQuery.Execute(null))
                .DisposeWith(this.DeactivateWith);
        }
    }
}
