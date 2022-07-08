using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public Graphic selectionGraphic;
    public List<SelectionGraphic> selectionGraphics;

    public UnityEvent pressEvent;
    public UnityEvent selectEvent;
    public UnityEvent deselectEvent;
    public Color selectedColor;

    public bool clickable = false;
    private Color defaultColor;
    [System.NonSerialized] public bool forceSelect = false;

    public List<Color> selectedColors;
    private Color defaultColors;
    [System.NonSerialized] public bool firstSelection = true;
    protected bool hovering = false;

    [System.NonSerialized] public ControllerMenu activeMenu;
    [System.NonSerialized] public int positionX;
    [System.NonSerialized] public int positionY;
    [System.NonSerialized] public bool selected;

    [System.Serializable]
    public class SelectionGraphic
    {
        public Graphic selectionGraphic;
        public Color selectionColor;
        [System.NonSerialized] public Color defaultColor;
    }


    public void Awake()
    {

    }

    public void OnDisable()
    {
        hovering = false;
    }

    public void Select()
    {
        if (!selected)
        {
            if (firstSelection)
            {
                firstSelection = false;
                if (selectionGraphic != null)
                {
                    defaultColor = selectionGraphic.color;
                }

                foreach (var graphic in selectionGraphics)
                {
                    graphic.defaultColor = graphic.selectionGraphic.color;
                }
            }

            selected = true;

            if (selectionGraphic != null)
            {
                selectionGraphic.color = selectedColor;
            }

            foreach (var graphic in selectionGraphics)
            {
                graphic.selectionGraphic.color = graphic.selectionColor;
            }

            ControllerMenu.selectedButton = this;
            selectEvent.Invoke();
        }
    }

    public void Deselect()
    {
        if (selected)
        {
            selected = false;

            if (selectionGraphic != null)
            {
                selectionGraphic.color = defaultColor;
            }

            foreach (var graphic in selectionGraphics)
            {
                graphic.selectionGraphic.color = graphic.defaultColor;
            }

            ControllerMenu.selectedButton = null;
            deselectEvent.Invoke();
        }
    }

    public void Press()
    {
        pressEvent.Invoke();
        OnPress();
    }

    public virtual void OnPress()
    {

    }

    public virtual void Update()
    {
        if (activeMenu != null && hovering && !selected)
        {
            activeMenu.MouseOverButton(positionX, positionY);
        }

        if (activeMenu != null && !hovering && selected && !forceSelect)
        {
            activeMenu.MouseExitButton(this);
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (activeMenu != null)
        {
            activeMenu.MouseClickButton(this);
        }
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
    }

    public virtual void NavigateLeft()
    {
        activeMenu.NavigateLeft();
    }

    public virtual void NavigateRight()
    {
        activeMenu.NavigateRight();
    }

    public virtual void NavigateUp()
    {
        activeMenu.NavigateUp();
    }

    public virtual void NavigateDown()
    {
        activeMenu.NavigateDown();
    }

    public virtual void NavigateLeftHold()
    {

    }

    public virtual void NavigateRightHold()
    {

    }

    public virtual void NavigateUpHold()
    {

    }

    public virtual void NavigateDownHold()
    {

    }
}
