﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Annotations;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditor.Managers;
using WoWDatabaseEditor.ViewModels;

namespace WoWDatabaseEditor.Providers
{
    [AutoRegister]
    public class EditorFileMenuItemProvider: IMainMenuItem, INotifyPropertyChanged
    {
        private readonly ISolutionManager solutionManager;
        private readonly IEventAggregator eventAggregator;
        private readonly INewItemService newItemService;
        private readonly IConfigureService settings;
        public IDocumentManager DocumentManager { get; }
        
        private readonly Dictionary<ISolutionItem, IDocument> documents = new();

        private readonly Dictionary<IDocument, ISolutionItem> documentToSolution = new();
        
        public string ItemName { get; } = "_File";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityVeryHigh;

        public EditorFileMenuItemProvider(ISolutionManager solutionManager, IEventAggregator eventAggregator, INewItemService newItemService, 
            ISolutionItemEditorRegistry solutionEditorManager, IMessageBoxService messageBoxService, IDocumentManager documentManager, IConfigureService settings)
        {
            this.solutionManager = solutionManager;
            this.eventAggregator = eventAggregator;
            this.newItemService = newItemService;
            DocumentManager = documentManager;
            this.settings = settings;
            SubItems = new List<IMenuItem>();
            SubItems.Add(new ModuleMenuItem("_New", new DelegateCommand(OpenNewItemWindow)));
            SubItems.Add(new ModuleMenuItem("_Save", new DelegateCommand(() => DocumentManager.ActiveDocument?.Save.Execute(null),
                () => DocumentManager.ActiveDocument?.Save.CanExecute(null) ?? false).ObservesProperty(() => DocumentManager.ActiveDocument).
                ObservesProperty(() => DocumentManager.ActiveDocument.IsModified)));
            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new ModuleMenuItem("_Settings", new DelegateCommand(OpenSettings)));
            SubItems.Add(new ModuleMenuItem("About", new DelegateCommand(OpenAbout)));
            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new ModuleMenuItem("_Exit", AlwaysDisabledCommand.Command));
            
            this.eventAggregator.GetEvent<DocumentManager.DocumentClosedEvent>()
                .Subscribe(document =>
                {
                    if (!documentToSolution.ContainsKey(document))
                        return;

                    documents.Remove(documentToSolution[document]);
                    documentToSolution.Remove(document);
                });

            this.eventAggregator.GetEvent<EventRequestOpenItem>()
                .Subscribe(item =>
                    {
                        if (documents.ContainsKey(item))
                            DocumentManager.OpenDocument(documents[item]);
                        else
                        {
                            try
                            {
                                IDocument editor = solutionEditorManager.GetEditor(item);
                                DocumentManager.OpenDocument(editor);
                                documents[item] = editor;
                                documentToSolution[editor] = item;
                            }
                            catch (SolutionItemEditorNotFoundException e)
                            {
                                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Editor not found")
                                    .SetMainInstruction(
                                        "Couldn't open item, because there is no editor registered for type " +
                                        item.GetType().Name)
#if DEBUG
                                    .SetContent(
                                        $"There should be class that implements ISolutionItemEditorProvider<{item.GetType().Name}> and this class should be registered in containerRegister in module.")
#endif
                                    .SetIcon(MessageBoxIcon.Warning)
                                    .WithOkButton(true)
                                    .Build());
                            }
                        }
                    },
                    true);
        }

        private void OpenNewItemWindow()
        {
            ISolutionItem? item = newItemService.GetNewSolutionItem();
            if (item != null)
            {
                solutionManager.Items.Add(item);
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
            }
        }
        private void OpenSettings() => settings.ShowSettings();

        private void OpenAbout() => DocumentManager.OpenDocument(new AboutViewModel());

        public event PropertyChangedEventHandler? PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}