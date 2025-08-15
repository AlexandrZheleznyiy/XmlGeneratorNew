using System;
using System.Collections.Generic;
using XmlGeneratorNew.Models;

namespace XmlGeneratorNew.Services
{
    public class UndoService
    {
        private readonly Stack<IUndoRedoAction> _undoStack = new Stack<IUndoRedoAction>();
        private readonly Stack<IUndoRedoAction> _redoStack = new Stack<IUndoRedoAction>();
        private bool _isUndoRedoing = false; // Флаг для предотвращения записи действий во время Undo/Redo

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event EventHandler? CanUndoChanged;
        public event EventHandler? CanRedoChanged;

        public void RecordAction(IUndoRedoAction action)
        {
            if (_isUndoRedoing) return;

            _undoStack.Push(action);
            _redoStack.Clear(); // Очищаем redo stack при новом действии
            OnCanUndoChanged();
            OnCanRedoChanged();
        }

        public void Undo()
        {
            if (!CanUndo) return;

            _isUndoRedoing = true;
            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
            _isUndoRedoing = false;

            OnCanUndoChanged();
            OnCanRedoChanged();
        }

        public void Redo()
        {
            if (!CanRedo) return;

            _isUndoRedoing = true;
            var action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
            _isUndoRedoing = false;

            OnCanUndoChanged();
            OnCanRedoChanged();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnCanUndoChanged();
            OnCanRedoChanged();
        }

        protected virtual void OnCanUndoChanged() => CanUndoChanged?.Invoke(this, EventArgs.Empty);
        protected virtual void OnCanRedoChanged() => CanRedoChanged?.Invoke(this, EventArgs.Empty);
    }

    public interface IUndoRedoAction
    {
        void Undo();
        void Redo();
    }

    // Действие: Добавление элемента
    public class AddItemAction : IUndoRedoAction
    {
        private readonly object _parent;
        private readonly object _item;
        private readonly int _index;

        public AddItemAction(object parent, object item, int index)
        {
            _parent = parent;
            _item = item;
            _index = index;
        }

        public void Undo()
        {
            switch (_parent)
            {
                case SectionItem section when _item is GroupItem:
                    section.Groups.RemoveAt(_index);
                    section.Children.Remove(_item);
                    break;
                case SectionItem section when _item is PropertyItem:
                    section.Properties.RemoveAt(_index);
                    section.Children.Remove(_item);
                    break;
                case GroupItem group when _item is GroupItem:
                    group.Groups.RemoveAt(_index);
                    group.Children.Remove(_item);
                    break;
                case GroupItem group when _item is PropertyItem:
                    group.Properties.RemoveAt(_index);
                    group.Children.Remove(_item);
                    break;
                // Для корневого уровня
                case System.Collections.IList rootList:
                    rootList.RemoveAt(_index);
                    break;
            }
        }

        public void Redo()
        {
            switch (_parent)
            {
                case SectionItem section when _item is GroupItem:
                    section.Groups.Insert(_index, (GroupItem)_item);
                    section.Children.Add(_item);
                    break;
                case SectionItem section when _item is PropertyItem:
                    section.Properties.Insert(_index, (PropertyItem)_item);
                    section.Children.Add(_item);
                    break;
                case GroupItem group when _item is GroupItem:
                    group.Groups.Insert(_index, (GroupItem)_item);
                    group.Children.Add(_item);
                    break;
                case GroupItem group when _item is PropertyItem:
                    group.Properties.Insert(_index, (PropertyItem)_item);
                    group.Children.Add(_item);
                    break;
                case System.Collections.IList rootList:
                    rootList.Insert(_index, _item);
                    break;
            }
        }
    }

    // Действие: Удаление элемента
    public class RemoveItemAction : IUndoRedoAction
    {
        private readonly object _parent;
        private readonly object _item;
        private readonly int _index;

        public RemoveItemAction(object parent, object item, int index)
        {
            _parent = parent;
            _item = item;
            _index = index;
        }

        public void Undo()
        {
            switch (_parent)
            {
                case SectionItem section when _item is GroupItem:
                    section.Groups.Insert(_index, (GroupItem)_item);
                    section.Children.Add(_item);
                    break;
                case SectionItem section when _item is PropertyItem:
                    section.Properties.Insert(_index, (PropertyItem)_item);
                    section.Children.Add(_item);
                    break;
                case GroupItem group when _item is GroupItem:
                    group.Groups.Insert(_index, (GroupItem)_item);
                    group.Children.Add(_item);
                    break;
                case GroupItem group when _item is PropertyItem:
                    group.Properties.Insert(_index, (PropertyItem)_item);
                    group.Children.Add(_item);
                    break;
                case System.Collections.IList rootList:
                    rootList.Insert(_index, _item);
                    break;
            }
        }

        public void Redo()
        {
            switch (_parent)
            {
                case SectionItem section when _item is GroupItem:
                    section.Groups.RemoveAt(_index);
                    section.Children.Remove(_item);
                    break;
                case SectionItem section when _item is PropertyItem:
                    section.Properties.RemoveAt(_index);
                    section.Children.Remove(_item);
                    break;
                case GroupItem group when _item is GroupItem:
                    group.Groups.RemoveAt(_index);
                    group.Children.Remove(_item);
                    break;
                case GroupItem group when _item is PropertyItem:
                    group.Properties.RemoveAt(_index);
                    group.Children.Remove(_item);
                    break;
                case System.Collections.IList rootList:
                    rootList.RemoveAt(_index);
                    break;
            }
        }
    }
}