﻿using System.Collections.ObjectModel;
using System.Windows.Media;
using WDE.Common;
using WDE.Common.Managers;

namespace WoWDatabaseEditor.Services.NewItemService
{
    public interface INewItemDialogViewModel : IDialog
    {
        ObservableCollection<NewItemPrototypeInfo> ItemPrototypes { get; }
        NewItemPrototypeInfo? SelectedPrototype { get; }
    }

    public class NewItemPrototypeInfo
    {
        private readonly ISolutionItemProvider provider;

        public NewItemPrototypeInfo(ISolutionItemProvider provider)
        {
            this.provider = provider;
            Name = provider.GetName();
            Description = provider.GetDescription();
            Image = provider.GetImage();
        }

        public string Name { get; }
        public string Description { get; }
        public ImageSource Image { get; }

        public ISolutionItem CreateSolutionItem()
        {
            return provider.CreateSolutionItem();
        }
    }
}