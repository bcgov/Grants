import { Directive, Input, ElementRef, OnDestroy, HostListener, Inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';

@Directive({
  selector: '[appTooltip]',
  standalone: true
})
export class TooltipDirective implements OnDestroy {
  @Input('appTooltip') tooltipText = '';
  @Input() tooltipPosition: 'top' | 'bottom' | 'left' | 'right' = 'top';

  private tooltipEl: HTMLElement | null = null;
  private tooltipId = '';
  private static nextId = 0;

  constructor(
    private readonly el: ElementRef<HTMLElement>,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
    this.tooltipId = `app-tooltip-${TooltipDirective.nextId++}`;
  }

  @HostListener('mouseenter')
  onMouseEnter(): void {
    if (!this.tooltipText) return;
    this.show();
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    this.hide();
  }

  @HostListener('focusin')
  onFocus(): void {
    if (!this.tooltipText) return;
    this.show();
  }

  @HostListener('focusout')
  onBlur(): void {
    this.hide();
  }

  ngOnDestroy(): void {
    this.hide();
  }

  private show(): void {
    if (this.tooltipEl) return;

    this.tooltipEl = this.document.createElement('div');
    this.tooltipEl.className = `app-tooltip app-tooltip--${this.tooltipPosition}`;
    this.tooltipEl.id = this.tooltipId;
    this.tooltipEl.setAttribute('role', 'tooltip');
    this.tooltipEl.textContent = this.tooltipText;

    const arrow = this.document.createElement('div');
    arrow.className = 'app-tooltip__arrow';
    this.tooltipEl.appendChild(arrow);

    this.document.body.appendChild(this.tooltipEl);
    this.el.nativeElement.setAttribute('aria-describedby', this.tooltipId);
    this.positionTooltip();
  }

  private hide(): void {
    if (this.tooltipEl) {
      this.tooltipEl.remove();
      this.tooltipEl = null;
      this.el.nativeElement.removeAttribute('aria-describedby');
    }
  }

  private positionTooltip(): void {
    if (!this.tooltipEl) return;

    const hostRect = this.el.nativeElement.getBoundingClientRect();
    const tooltipRect = this.tooltipEl.getBoundingClientRect();
    const win = this.document.defaultView;
    const scrollX = win?.scrollX ?? 0;
    const scrollY = win?.scrollY ?? 0;
    const gap = 8;

    let top = 0;
    let left = 0;

    switch (this.tooltipPosition) {
      case 'top':
        top = hostRect.top + scrollY - tooltipRect.height - gap;
        left = hostRect.left + scrollX + (hostRect.width - tooltipRect.width) / 2;
        break;
      case 'bottom':
        top = hostRect.bottom + scrollY + gap;
        left = hostRect.left + scrollX + (hostRect.width - tooltipRect.width) / 2;
        break;
      case 'left':
        top = hostRect.top + scrollY + (hostRect.height - tooltipRect.height) / 2;
        left = hostRect.left + scrollX - tooltipRect.width - gap;
        break;
      case 'right':
        top = hostRect.top + scrollY + (hostRect.height - tooltipRect.height) / 2;
        left = hostRect.right + scrollX + gap;
        break;
    }

    this.tooltipEl.style.top = `${top}px`;
    this.tooltipEl.style.left = `${left}px`;
  }
}
