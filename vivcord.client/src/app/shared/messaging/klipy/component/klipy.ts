import {
  Component,
  inject,
  signal,
  output,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Klipy } from '../service/klipy.service';
import { GifDTO, GifPagedResult } from '../dto/klipy.dto';

interface GifItem {
  url: string;
  preview: string;
  title: string;
}

@Component({
  selector: 'app-klipy',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './klipy.html',
  styleUrl: './klipy.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KlipyComponent implements OnInit {
  private readonly klipyService = inject(Klipy);

  /** Emitted when the user clicks a GIF — parent should send it as a message */
  public readonly gifSelected = output<string>();

  /** Emitted when the widget wants to close itself */
  public readonly closed = output<void>();

  public readonly gifs = signal<GifItem[]>([]);
  public readonly isLoading = signal(false);
  public readonly error = signal<string | null>(null);
  public readonly searchQuery = signal('');

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadTrending();
  }

  public onSearch(query: string): void {
    this.searchQuery.set(query);

    if (this.searchDebounce) clearTimeout(this.searchDebounce);

    if (!query.trim()) {
      this.loadTrending();
      return;
    }

    this.searchDebounce = setTimeout(() => this.loadSearch(query.trim()), 400);
  }

  public selectGif(url: string): void {
    this.gifSelected.emit(url);
    this.closed.emit();
  }

  public close(): void {
    this.closed.emit();
  }

  private loadTrending(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.klipyService.getTrendingGifs().subscribe({
      next: (res: GifPagedResult) => {
        this.gifs.set(this.mapResponse(res));
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load GIFs');
        this.isLoading.set(false);
      },
    });
  }

  private loadSearch(query: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.klipyService.searchGifs(query).subscribe({
      next: (res: GifPagedResult) => {
        this.gifs.set(this.mapResponse(res));
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Search failed');
        this.isLoading.set(false);
      },
    });
  }

  private mapResponse(res: GifPagedResult): GifItem[] {
    return res.items
      .filter((item: GifDTO) => !!item.originalUrl)
      .map((item: GifDTO): GifItem => ({
        url: item.originalUrl,
        preview: item.previewUrl || item.originalUrl,
        title: item.name,
      }));
  }
}
