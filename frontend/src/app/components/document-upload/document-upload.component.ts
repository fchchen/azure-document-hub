import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
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
  template: `
    <div class="upload-container"
         (dragover)="onDragOver($event)"
         (dragleave)="onDragLeave($event)"
         (drop)="onDrop($event)"
         [class.drag-over]="isDragOver()">

      <input type="file" #fileInput (change)="onFileSelected($event)" hidden
             accept=".pdf,.jpg,.jpeg,.png,.gif,.doc,.docx">

      <div class="upload-content">
        <mat-icon class="upload-icon">cloud_upload</mat-icon>
        <h3>Drag & drop files here</h3>
        <p>or</p>
        <button mat-raised-button color="primary" (click)="fileInput.click()" [disabled]="uploading()">
          Browse Files
        </button>
        <p class="file-types">Supported: PDF, JPEG, PNG, GIF, DOC, DOCX (Max 50MB)</p>
      </div>

      @if (uploading()) {
        <mat-progress-bar mode="indeterminate"></mat-progress-bar>
      }

      @if (selectedFile()) {
        <div class="selected-file">
          <mat-icon>description</mat-icon>
          <span>{{ selectedFile()?.name }}</span>
          <span class="file-size">({{ formatFileSize(selectedFile()?.size || 0) }})</span>
          <button mat-icon-button (click)="clearSelection()" [disabled]="uploading()">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .upload-container {
      border: 2px dashed #ccc;
      border-radius: 8px;
      padding: 40px;
      text-align: center;
      transition: all 0.3s ease;
      margin: 16px;
      background-color: #fafafa;
    }

    .upload-container.drag-over {
      border-color: #1976d2;
      background-color: #e3f2fd;
    }

    .upload-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    .upload-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #1976d2;
    }

    .file-types {
      color: #666;
      font-size: 12px;
      margin-top: 16px;
    }

    .selected-file {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      margin-top: 16px;
      padding: 8px 16px;
      background-color: #e8f5e9;
      border-radius: 4px;
    }

    .file-size {
      color: #666;
    }

    mat-progress-bar {
      margin-top: 16px;
    }
  `]
})
export class DocumentUploadComponent {
  @Output() uploaded = new EventEmitter<void>();

  private readonly documentService = inject(DocumentService);
  private readonly snackBar = inject(MatSnackBar);

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

  private readonly maxSize = 50 * 1024 * 1024; // 50MB

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  private handleFile(file: File) {
    if (!this.allowedTypes.includes(file.type)) {
      this.snackBar.open('File type not allowed', 'Close', { duration: 3000 });
      return;
    }

    if (file.size > this.maxSize) {
      this.snackBar.open('File size exceeds 50MB limit', 'Close', { duration: 3000 });
      return;
    }

    this.selectedFile.set(file);
    this.uploadFile(file);
  }

  private uploadFile(file: File) {
    this.uploading.set(true);

    this.documentService.uploadDocument(file).subscribe({
      next: (response) => {
        this.snackBar.open(`Document "${response.fileName}" uploaded successfully`, 'Close', { duration: 3000 });
        this.uploading.set(false);
        this.selectedFile.set(null);
        this.uploaded.emit();
      },
      error: (err) => {
        this.snackBar.open('Failed to upload document', 'Close', { duration: 3000 });
        this.uploading.set(false);
      }
    });
  }

  clearSelection() {
    this.selectedFile.set(null);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
