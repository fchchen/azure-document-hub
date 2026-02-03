import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DocumentService } from '../../services/document.service';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule
  ],
  templateUrl: './document-upload.component.html',
  styleUrl: './document-upload.component.scss'
})
export class DocumentUploadComponent {
  // New output() syntax - signal-based
  uploaded = output<void>();

  private readonly documentService = inject(DocumentService);
  private readonly snackBar = inject(MatSnackBar);

  // Signals for reactive state
  selectedFile = signal<File | null>(null);
  uploading = signal(false);
  isDragOver = signal(false);

  private readonly allowedTypes = [
    'application/pdf',
    'image/jpeg',
    'image/png',
    'image/gif',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
  ];

  private readonly maxSize = 500 * 1024; // 500KB

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  clearSelection(): void {
    this.selectedFile.set(null);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  private handleFile(file: File): void {
    if (!this.allowedTypes.includes(file.type)) {
      this.snackBar.open('File type not allowed', 'Close', { duration: 3000 });
      return;
    }

    if (file.size > this.maxSize) {
      this.snackBar.open('File size exceeds 500KB limit', 'Close', { duration: 3000 });
      return;
    }

    this.selectedFile.set(file);
    this.uploadFile(file);
  }

  private uploadFile(file: File): void {
    this.uploading.set(true);

    this.documentService.uploadDocument(file).subscribe({
      next: (response) => {
        this.snackBar.open(`Document "${response.fileName}" uploaded successfully`, 'Close', { duration: 3000 });
        this.uploading.set(false);
        this.selectedFile.set(null);
        this.uploaded.emit();
      },
      error: () => {
        this.snackBar.open('Failed to upload document', 'Close', { duration: 3000 });
        this.uploading.set(false);
      }
    });
  }
}
