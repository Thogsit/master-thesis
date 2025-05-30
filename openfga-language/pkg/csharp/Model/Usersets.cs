//
// OpenFGA/.NET SDK for OpenFGA
//
// API version: 1.x
// Website: https://openfga.dev
// Documentation: https://openfga.dev/docs
// Support: https://openfga.dev/community
// License: [Apache-2.0](https://github.com/openfga/dotnet-sdk/blob/main/LICENSE)
//
// NOTE: This file was auto generated. DO NOT EDIT.
//


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenFga.Language.Model {
    /// <summary>
    /// Usersets
    /// </summary>
    [DataContract(Name = "Usersets")]
    public class Usersets : IEquatable<Usersets>, IValidatableObject {
        /// <summary>
        /// Initializes a new instance of the <see cref="Usersets" /> class.
        /// </summary>
        [JsonConstructor]
        public Usersets() {
            this.AdditionalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Usersets" /> class.
        /// </summary>
        /// <param name="child">child (required).</param>
        public Usersets(List<Userset> child = null) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            AdditionalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or Sets Child
        /// </summary>
        [DataMember(Name = "child", IsRequired = true, EmitDefaultValue = false)]
        [JsonPropertyName("child")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<Userset> Child { get; set; }

        /// <summary>
        /// Gets or Sets additional properties
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; set; }


        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson() {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Builds a Usersets from the JSON string presentation of the object
        /// </summary>
        /// <returns>Usersets</returns>
        public static Usersets FromJson(string jsonString) {
            return JsonSerializer.Deserialize<Usersets>(jsonString) ?? throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input) {
            return this.Equals(input as Usersets);
        }

        /// <summary>
        /// Returns true if Usersets instances are equal
        /// </summary>
        /// <param name="input">Instance of Usersets to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Usersets input) {
            if (input == null) {
                return false;
            }
            return
                (
                    this.Child == input.Child ||
                    this.Child != null &&
                    input.Child != null &&
                    this.Child.SequenceEqual(input.Child)
                )
                && (this.AdditionalProperties.Count == input.AdditionalProperties.Count && !this.AdditionalProperties.Except(input.AdditionalProperties).Any());
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode() {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 9661;
                if (this.Child != null) {
                    hashCode = (hashCode * 9923) + this.Child.GetHashCode();
                }
                if (this.AdditionalProperties != null) {
                    hashCode = (hashCode * 9923) + this.AdditionalProperties.GetHashCode();
                }
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            yield break;
        }

    }

}