using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Layouts;

namespace Maui.Controls.Sample;

public class FlexLayoutViewModel : INotifyPropertyChanged
{
    private FlexDirection _direction = FlexDirection.Row;
    private FlexJustify _justifyContent = FlexJustify.Start;
    private FlexAlignContent _alignContent = FlexAlignContent.Stretch;
    private FlexAlignItems _alignItems = FlexAlignItems.Stretch;
    private FlexPosition _position = FlexPosition.Relative;
    private FlexWrap _wrap = FlexWrap.NoWrap;

    // Child-specific properties for testing attached properties
    private float _child1Grow = 0f;
    private float _child2Grow = 0f;
    private float _child3Grow = 0f;

    private float _child1Shrink = 1f;
    private float _child2Shrink = 1f;
    private float _child3Shrink = 1f;

    private int _child1Order = 0;
    private int _child2Order = 0;
    private int _child3Order = 0;

    private FlexAlignSelf _child1AlignSelf = FlexAlignSelf.Auto;
    private FlexAlignSelf _child2AlignSelf = FlexAlignSelf.Auto;
    private FlexAlignSelf _child3AlignSelf = FlexAlignSelf.Auto;

    private FlexBasis _child1Basis = FlexBasis.Auto;
    private FlexBasis _child2Basis = FlexBasis.Auto;
    private FlexBasis _child3Basis = FlexBasis.Auto;

    public FlexDirection Direction
    {
        get => _direction;
        set
        {
            if (_direction != value)
            {
                _direction = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexJustify JustifyContent
    {
        get => _justifyContent;
        set
        {
            if (_justifyContent != value)
            {
                _justifyContent = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexAlignContent AlignContent
    {
        get => _alignContent;
        set
        {
            if (_alignContent != value)
            {
                _alignContent = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexAlignItems AlignItems
    {
        get => _alignItems;
        set
        {
            if (_alignItems != value)
            {
                _alignItems = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexPosition Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexWrap Wrap
    {
        get => _wrap;
        set
        {
            if (_wrap != value)
            {
                _wrap = value;
                OnPropertyChanged();
            }
        }
    }

    // Child-specific properties
    public float Child1Grow
    {
        get => _child1Grow;
        set
        {
            if (_child1Grow != value)
            {
                _child1Grow = value;
                OnPropertyChanged();
            }
        }
    }

    public float Child2Grow
    {
        get => _child2Grow;
        set
        {
            if (_child2Grow != value)
            {
                _child2Grow = value;
                OnPropertyChanged();
            }
        }
    }

    public float Child3Grow
    {
        get => _child3Grow;
        set
        {
            if (_child3Grow != value)
            {
                _child3Grow = value;
                OnPropertyChanged();
            }
        }
    }

    public float Child1Shrink
    {
        get => _child1Shrink;
        set
        {
            if (_child1Shrink != value)
            {
                _child1Shrink = value;
                OnPropertyChanged();
            }
        }
    }

    public float Child2Shrink
    {
        get => _child2Shrink;
        set
        {
            if (_child2Shrink != value)
            {
                _child2Shrink = value;
                OnPropertyChanged();
            }
        }
    }

    public float Child3Shrink
    {
        get => _child3Shrink;
        set
        {
            if (_child3Shrink != value)
            {
                _child3Shrink = value;
                OnPropertyChanged();
            }
        }
    }

    public int Child1Order
    {
        get => _child1Order;
        set
        {
            if (_child1Order != value)
            {
                _child1Order = value;
                OnPropertyChanged();
            }
        }
    }

    public int Child2Order
    {
        get => _child2Order;
        set
        {
            if (_child2Order != value)
            {
                _child2Order = value;
                OnPropertyChanged();
            }
        }
    }

    public int Child3Order
    {
        get => _child3Order;
        set
        {
            if (_child3Order != value)
            {
                _child3Order = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexAlignSelf Child1AlignSelf
    {
        get => _child1AlignSelf;
        set
        {
            if (_child1AlignSelf != value)
            {
                _child1AlignSelf = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexAlignSelf Child2AlignSelf
    {
        get => _child2AlignSelf;
        set
        {
            if (_child2AlignSelf != value)
            {
                _child2AlignSelf = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexAlignSelf Child3AlignSelf
    {
        get => _child3AlignSelf;
        set
        {
            if (_child3AlignSelf != value)
            {
                _child3AlignSelf = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexBasis Child1Basis
    {
        get => _child1Basis;
        set
        {
            if (_child1Basis != value)
            {
                _child1Basis = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexBasis Child2Basis
    {
        get => _child2Basis;
        set
        {
            if (_child2Basis != value)
            {
                _child2Basis = value;
                OnPropertyChanged();
            }
        }
    }

    public FlexBasis Child3Basis
    {
        get => _child3Basis;
        set
        {
            if (_child3Basis != value)
            {
                _child3Basis = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
