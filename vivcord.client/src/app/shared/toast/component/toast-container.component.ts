import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ToastService } from '../service/toast.service';
import { Toast } from '../dto/toast.dto';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastContainerComponent {
  public readonly toastService = inject(ToastService);

  public handleToastClick(toast: Toast): void {
    if (toast.onClick) {
      toast.onClick();
    }
    this.toastService.dismiss(toast.id);
  }
}
