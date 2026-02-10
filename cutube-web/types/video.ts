export interface VideoMetadata {
  url: string;
  title: string;
  uploader?: string | null;
  duration: string;
  thumbnailUrl: string;
  uploadDate?: string;
  formats: VideoFormat[];
}

export interface VideoFormat {
  formatId: string;
  extension: string;
  resolution?: string;
  fileSize?: number;
}
