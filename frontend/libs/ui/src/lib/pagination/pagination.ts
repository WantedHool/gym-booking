export const DEFAULT_PAGE_SIZE = 10;
export const PAGE_SIZE_OPTIONS = [5, 10, 25, 50] as const;

export interface ResponsivePageSize {
  mobile: number;
  desktop: number;
}

export interface PaginationStateOptions {
  pageSize?: number | ResponsivePageSize;
  pageSizeOptions?: readonly number[];
}

export function paginatedSlice<T>(items: readonly T[], pageIndex: number, pageSize: number): T[] {
  const start = pageIndex * pageSize;
  return items.slice(start, start + pageSize);
}

export function maxPageIndex(itemCount: number, pageSize: number): number {
  return Math.max(0, Math.ceil(itemCount / pageSize) - 1);
}

export function isResponsivePageSize(
  pageSize: number | ResponsivePageSize | undefined,
): pageSize is ResponsivePageSize {
  return typeof pageSize === 'object' && pageSize !== null;
}

export const MOBILE_BREAKPOINT = '(max-width: 639px)';

export function resolvePageSize(
  pageSize: number | ResponsivePageSize | undefined,
  isMobile: boolean,
): number {
  if (!pageSize) {
    return DEFAULT_PAGE_SIZE;
  }
  if (typeof pageSize === 'number') {
    return pageSize;
  }
  return isMobile ? pageSize.mobile : pageSize.desktop;
}
