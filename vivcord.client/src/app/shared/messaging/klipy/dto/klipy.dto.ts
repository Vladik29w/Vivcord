export interface GifDTO {
  id: string;
  name: string;
  previewUrl: string;
  originalUrl: string;
  width: number;
  height: number;
}

export interface PagedResultDto<T> {
  items: T[];
  page: number;
  hasNext: boolean;
}

export type GifPagedResult = PagedResultDto<GifDTO>;
